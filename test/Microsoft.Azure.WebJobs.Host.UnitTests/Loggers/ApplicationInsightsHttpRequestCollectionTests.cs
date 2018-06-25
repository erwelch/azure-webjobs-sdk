// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Logging.ApplicationInsights;
using Xunit;

namespace Microsoft.Azure.WebJobs.Host.UnitTests.Loggers
{
    public class ApplicationInsightsHttpRequestCollectionTests
    {
        private const string _mockInstrumentationKey = "some-ikey";
        private const string RequestContextHeaderName = "Request-Context";

        private readonly TelemetryClient _telemetryClient;
        private readonly TestTelemetryChannel _telemetryChannel = new TestTelemetryChannel();
        private readonly Uri _mockUri = new Uri("https://host.com:1234/path?query");
        private readonly IApplicationIdProvider _appIdProvider = new TestApplicationIdProvider();
        private readonly string _mockApplicationId;
        private readonly TelemetryConfiguration _configuration;
        public ApplicationInsightsHttpRequestCollectionTests()
        {
            _configuration = new TelemetryConfiguration(_mockInstrumentationKey) {TelemetryChannel = _telemetryChannel};
            _telemetryClient = new TelemetryClient(_configuration);
            _appIdProvider.TryGetApplicationId(_mockInstrumentationKey, out _mockApplicationId);
        }

        [Fact]
        public void RequestStartStopTracksRequest()
        {
            var httpContext = CreateHttpContext(headers: null);
            Activity requestActivity = new Activity("dummy").Start();
            using (var listener = new RequestTrackingTelemetryModule(_appIdProvider, true))
            {
                listener.Initialize(_configuration);
                listener.OnHttpRequestInStart(httpContext);

                SetResponse(httpContext, HttpStatusCode.OK);
                listener.OnHttpRequestInStop(httpContext);
            }
            requestActivity.Stop();

            var requests = _telemetryChannel.Telemetries.OfType<RequestTelemetry>().ToArray();
            Assert.Single(requests);
            ValidateRequest(requests.Single(), requestActivity, HttpStatusCode.OK, string.Empty);

            Assert.Equal($"appId=cid-v1:{_mockApplicationId}", httpContext.Response.Headers[RequestContextHeaderName]);
        }

        [Fact]
        public void TrackRequestWithTheSameAppId()
        {
            var httpContext = CreateHttpContext(headers: new Dictionary<string, string>
            {
                ["Request-Context"] = $"appId=cid-v1:{_mockApplicationId}"
            });

            Activity requestActivity = new Activity("dummy").Start();
            using (var listener = new RequestTrackingTelemetryModule(_appIdProvider, true))
            {
                listener.Initialize(_configuration);
                listener.OnHttpRequestInStart(httpContext);

                SetResponse(httpContext, HttpStatusCode.OK);
                listener.OnHttpRequestInStop(httpContext);
            }
            requestActivity.Stop();

            var requests = _telemetryChannel.Telemetries.OfType<RequestTelemetry>().ToArray();
            Assert.Single(requests);
            ValidateRequest(requests.Single(), requestActivity, HttpStatusCode.OK, string.Empty);

            Assert.Equal($"appId=cid-v1:{_mockApplicationId}", httpContext.Response.Headers[RequestContextHeaderName]);
        }

        [Fact]
        public void TrackRequestWithAnotherAppId()
        {
            string anotherAppId = "another-appId";
            var httpContext = CreateHttpContext(headers: new Dictionary<string, string>
            {
                ["Request-Context"] = $"appId=cid-v1:{anotherAppId}"
            });

            Activity requestActivity = new Activity("dummy").Start();
            using (var listener = new RequestTrackingTelemetryModule(_appIdProvider, true))
            {
                listener.Initialize(_configuration);
                listener.OnHttpRequestInStart(httpContext);

                SetResponse(httpContext, HttpStatusCode.OK);
                listener.OnHttpRequestInStop(httpContext);
            }
            requestActivity.Stop();

            var requests = _telemetryChannel.Telemetries.OfType<RequestTelemetry>().ToArray();
            Assert.Single(requests);
            ValidateRequest(requests.Single(), requestActivity, HttpStatusCode.OK, $"cid-v1:{anotherAppId}");
            Assert.Equal($"appId=cid-v1:{_mockApplicationId}", httpContext.Response.Headers[RequestContextHeaderName]);
        }

        [Fact]
        public void RequestStartStopTracksRequestWithW3CContext()
        {
            const string version = "00",
                traceId = "0af7651916cd43dd8448eb211c80319c",
                spanId = "b9c7c989f97918e1",
                sampled = "01",
                tracestate = "key=value";

            var httpContext = CreateHttpContext(headers: new Dictionary<string, string>
            {
                ["traceparent"] = $"{version}-{traceId}-{spanId}-{sampled}",
                ["tracestate"] = tracestate
            });

            Activity requestActivity = new Activity("dummy")
                .Start()
                .AddBaggage("baggageKey", "vaggageValue");

            using (var listener = new RequestTrackingTelemetryModule(_appIdProvider, true))
            {
                listener.Initialize(_configuration);
                listener.OnHttpRequestInStart(httpContext);

                SetResponse(httpContext, HttpStatusCode.OK);
                listener.OnHttpRequestInStop(httpContext);
            }
            requestActivity.Stop();

            var requests = _telemetryChannel.Telemetries.OfType<RequestTelemetry>().ToArray();
            Assert.Single(requests);
            ValidateRequest(requests.Single(), requestActivity, HttpStatusCode.OK, string.Empty);
            ValidateW3CActivity(requestActivity, version, traceId, spanId, sampled, tracestate);
            Assert.Equal($"appId=cid-v1:{_mockApplicationId}", httpContext.Response.Headers[RequestContextHeaderName]);
        }

        [Fact]
        public void RequestStartStopTracksRequestNoBackPropagation()
        {
            var httpContext = CreateHttpContext(headers: null);
            Activity requestActivity = new Activity("dummy").Start();
            using (var listener = new RequestTrackingTelemetryModule(_appIdProvider, false))
            {
                listener.Initialize(_configuration);
                listener.OnHttpRequestInStart(httpContext);

                SetResponse(httpContext, HttpStatusCode.OK);
                listener.OnHttpRequestInStop(httpContext);
            }
            requestActivity.Stop();

            var requests = _telemetryChannel.Telemetries.OfType<RequestTelemetry>().ToArray();
            Assert.Single(requests);
            ValidateRequest(requests.Single(), requestActivity, HttpStatusCode.OK, string.Empty);

            Assert.DoesNotContain(httpContext.Response.Headers, h => h.Key == RequestContextHeaderName);
        }

        private static void TestFunction()
        {
            // used for a FunctionDescriptor
        }

        private HttpContext CreateHttpContext(IDictionary<string, string> headers)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Host = HostString.FromUriComponent(_mockUri);
            httpContext.Request.Scheme = _mockUri.Scheme;
            httpContext.Request.Method = "GET";
            httpContext.Request.Path = PathString.FromUriComponent(_mockUri);
            httpContext.Request.QueryString = QueryString.FromUriComponent(_mockUri);

            if (headers != null)
            {
                foreach (var h in headers)
                {
                    httpContext.Request.Headers.Append(h.Key, h.Value);
                }
            }

            return httpContext;
        }

        private void SetResponse(HttpContext httpContext, HttpStatusCode status)
        {
            httpContext.Response.StatusCode = (int)status;
        }

        private void ValidateRequest(RequestTelemetry telemetry, Activity activity, HttpStatusCode statusCode, string source)
        {
            Assert.Equal(activity.Id, telemetry.Id);
            Assert.Equal(activity.RootId, telemetry.Context.Operation.Id);
            Assert.Equal(activity.ParentId, telemetry.Context.Operation.ParentId);

            // Operation Name and Name are set by WebJobTelmeertyIntitializer, which is not part of this
            Assert.True(string.IsNullOrEmpty(telemetry.Context.Operation.Name));
            Assert.True(string.IsNullOrEmpty(telemetry.Name));

            foreach (var baggageItem in activity.Baggage)
            {
                Assert.Equal(baggageItem.Value, telemetry.Properties[baggageItem.Key]);
            }

            Assert.Equal(((int)statusCode).ToString(), telemetry.ResponseCode);
            Assert.Equal(source, telemetry.Source);
            Assert.Equal(_mockUri, telemetry.Url);
            Assert.Equal(LoggingConstants.SdkVersion, telemetry.Context.GetInternalContext().SdkVersion);
        }

        private void ValidateW3CActivity(Activity requestActivity, string version, string traceId, string spanId,
            string sampled, string traceState)
        {
            Assert.Equal(version, requestActivity.Tags.Single(t => t.Key == LoggingConstants.W3CVersionTag).Value);
            Assert.Equal(traceId, requestActivity.Tags.Single(t => t.Key == LoggingConstants.W3CTraceIdTag).Value);
            Assert.Equal(spanId, requestActivity.Tags.Single(t => t.Key == LoggingConstants.W3CSpanIdTag).Value);
            Assert.Equal(sampled, requestActivity.Tags.Single(t => t.Key == LoggingConstants.W3CSampledTag).Value);
            Assert.Equal(traceState, requestActivity.Tags.Single(t => t.Key == LoggingConstants.W3CTraceStateTag).Value);
        }
    }
}
