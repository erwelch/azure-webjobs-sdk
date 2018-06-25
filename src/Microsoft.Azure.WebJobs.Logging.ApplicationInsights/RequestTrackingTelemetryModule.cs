// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DiagnosticAdapter;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Azure.WebJobs.Logging.ApplicationInsights
{
    /// <summary>
    /// Implements listener for ASP.NET Events this could be removed later when
    /// W3C distributed tracing standard is implemented in ASP.NET Core and
    /// https://github.com/Microsoft/ApplicationInsights-aspnetcore/issues/709 is fixed
    /// </summary>
    internal class RequestTrackingTelemetryModule : ITelemetryModule, IDisposable
    {
        private const string AspNetDiagnosticSourceName = "Microsoft.AspNetCore";

        private const string RequestContextHeader = "Request-Context";
        private const string ApplicationIdPropertyName = "appId";
        private const string ApplicationIdSchema = "cid-v1:";
        private const string TraceParentHeader = "traceparent";
        private const string TraceStateHeader = "tracestate";

        private readonly IApplicationIdProvider _applicationIdProvider;
        private readonly bool _enableResponseHeaderBackPropagation;

        private TelemetryClient _telemetryClient;
        private string _instrumentationKey;
        private IDisposable _allListenerSubscription;
        private Subscriber _subscriber;

        private bool _isInitialized = false;
        private readonly object _lockObject = new object();

        public RequestTrackingTelemetryModule(
            IApplicationIdProvider applicationIdProvider,
            bool enableResponseHeaderBackPropagation)
        {
            _applicationIdProvider = applicationIdProvider;
            _enableResponseHeaderBackPropagation = enableResponseHeaderBackPropagation;
        }

        public void Initialize(TelemetryConfiguration configuration)
        {
            if (!_isInitialized)
            {
                lock (_lockObject)
                {
                    if (!_isInitialized)
                    {
                        _instrumentationKey = configuration.InstrumentationKey;
                        _telemetryClient = new TelemetryClient(configuration);
                        _telemetryClient.Context.GetInternalContext().SdkVersion = LoggingConstants.SdkVersion;

                        _subscriber = new Subscriber(this);
                        _allListenerSubscription = DiagnosticListener.AllListeners.Subscribe(_subscriber);
                        _isInitialized = true;
                    }
                }
            }
        }

        [DiagnosticName("Microsoft.AspNetCore.Hosting.HttpRequestIn")]
        public void OnHttpRequestIn()
        {
            // do nothing, just enable the diagnotic source
        }

        [DiagnosticName("Microsoft.AspNetCore.Hosting.HttpRequestIn.Start")]
        public void OnHttpRequestInStart(HttpContext httpContext)
        {
            Activity currentActivity = Activity.Current;

            if (httpContext != null && currentActivity != null)
            {
                httpContext.Features.Set(CreateRequestTelemetry(httpContext.Request, currentActivity));
                ParseW3CHeaders(httpContext.Request.Headers, currentActivity);

                if (_applicationIdProvider != null && _enableResponseHeaderBackPropagation)
                {
                    SetAppIdInResponseHeader(httpContext.Response?.Headers);
                }
            }
        }

        [DiagnosticName("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop")]
        public void OnHttpRequestInStop(HttpContext httpContext)
        {
            RequestTelemetry requestTelemetry = httpContext?.Features.Get<RequestTelemetry>();

            if (requestTelemetry == null)
            {
                return;
            }

            requestTelemetry.Stop();
            requestTelemetry.ResponseCode = httpContext.Response.StatusCode.ToString();

            var successExitCode = httpContext.Response.StatusCode < 400;
            requestTelemetry.Success = successExitCode;
            requestTelemetry.Url = new Uri(httpContext.Request.GetDisplayUrl());

            _telemetryClient.TrackRequest(requestTelemetry);
        }

        private void SetAppIdInResponseHeader(IHeaderDictionary responseHeaders)
        {
            Debug.Assert(_applicationIdProvider != null);
            if (!responseHeaders[RequestContextHeader].Any(v => v.StartsWith(ApplicationIdPropertyName)))
            {
                if (_applicationIdProvider.TryGetApplicationId(_instrumentationKey, out string applicationId))
                {
                    responseHeaders.Append(RequestContextHeader, $"{ApplicationIdPropertyName}={ApplicationIdSchema}{applicationId}");
                }
            }
        }

        private bool TryGetAppIdFromRequestHeader(IHeaderDictionary requestHeaders, out string appId)
        {
            Debug.Assert(_applicationIdProvider != null);
            appId = null;
            if (requestHeaders.TryGetValue(RequestContextHeader, out StringValues requestContext))
            {
                string headerAppId = requestContext.FirstOrDefault(h => h.StartsWith(ApplicationIdPropertyName));

                if (TryParseApplicationId(headerAppId, out string senderApplicationId) &&
                    _applicationIdProvider.TryGetApplicationId(_instrumentationKey, out string myApplicationId) &&
                    ApplicationIdSchema + myApplicationId != senderApplicationId)
                {
                    appId = senderApplicationId;
                    return true;
                }
            }

            return false;
        }

        private RequestTelemetry CreateRequestTelemetry(HttpRequest httpRequest, Activity currentActivity)
        {
            var requestTelemetry = new RequestTelemetry
            {
                Id = currentActivity.Id
            };

            requestTelemetry.Start();

            requestTelemetry.Context.Operation.ParentId = currentActivity.ParentId;
            requestTelemetry.Context.Operation.Id = currentActivity.RootId;

            foreach (var prop in currentActivity.Baggage)
            {
                if (!requestTelemetry.Properties.ContainsKey(prop.Key))
                {
                    requestTelemetry.Properties[prop.Key] = prop.Value;
                }
            }

            if (_applicationIdProvider != null && TryGetAppIdFromRequestHeader(httpRequest.Headers, out string appId))
            {
                requestTelemetry.Source = appId;
            }

            return requestTelemetry;
        }

        private void ParseW3CHeaders(IHeaderDictionary requestHeaders, Activity currentActivity)
        {
            if (requestHeaders.TryGetValue(TraceParentHeader, out StringValues parent))
            {
                string traceparent = parent.First();
                string[] parts = traceparent.Split('-');
                if (parts.Length == 4)
                {
                    currentActivity.AddTag(LoggingConstants.W3CVersionTag, parts[0]);
                    currentActivity.AddTag(LoggingConstants.W3CTraceIdTag, parts[1]);
                    currentActivity.AddTag(LoggingConstants.W3CSpanIdTag, parts[2]);
                    currentActivity.AddTag(LoggingConstants.W3CSampledTag, parts[3]);
                }
            }

            if (requestHeaders.TryGetValue(TraceStateHeader, out StringValues state))
            {
                currentActivity.AddTag(LoggingConstants.W3CTraceStateTag, string.Join(",", state));
            }
        }

        private bool TryParseApplicationId(string appIdPair, out string applicaitonId)
        {
            string[] kvp = appIdPair.Split('=');
            if (kvp.Length == 2)
            {
                applicaitonId = kvp[1];
                return true;
            }

            applicaitonId = null;
            return false;
        }

        public void Dispose()
        {
            _subscriber?.Dispose();
            _allListenerSubscription?.Dispose();
        }

        private class Subscriber : IObserver<DiagnosticListener>, IDisposable
        {
            private readonly RequestTrackingTelemetryModule _listener;
            private IDisposable _subscription;

            public Subscriber(RequestTrackingTelemetryModule listener)
            {
                _listener = listener;
            }

            public void OnNext(DiagnosticListener value)
            {
                if (_subscription == null && value.Name == AspNetDiagnosticSourceName)
                {
                    _subscription = value.SubscribeWithAdapter(_listener);
                }
            }

            public void OnCompleted() {}

            public void OnError(Exception error) {}

            public void Dispose()
            {
                _subscription?.Dispose();
            }
        }
    }
}
