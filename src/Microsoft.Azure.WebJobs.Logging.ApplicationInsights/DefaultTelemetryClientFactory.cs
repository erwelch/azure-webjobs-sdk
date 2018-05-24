// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.ApplicationInsights.WindowsServer.Channel.Implementation;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Logging.ApplicationInsights
{

    public static class WebJobsHostLoggingExtensions
    {

        public static IHostBuilder ConfigureApplicationInsights(this IHostBuilder builder)
        {
            string instrumentationKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");

            if (!string.IsNullOrEmpty(instrumentationKey))
            {
                var filter = new LogCategoryFilter
                {
                    DefaultLevel = LogLevel.Debug
                };

                filter.CategoryLevels[LogCategories.Results] = LogLevel.Debug;
                filter.CategoryLevels[LogCategories.Aggregator] = LogLevel.Debug;

                SamplingPercentageEstimatorSettings samplingSettings = new SamplingPercentageEstimatorSettings();

                builder.ConfigureServices((context, services) =>
                {
                    //services.AddSingleton<ITelemetryInitializer, Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers.OperationCorrelationTelemetryInitializer>();
                    //services.AddSingleton<ITelemetryInitializer, AspNetCoreEnvironmentTelemetryInitializer>();
                    services.AddSingleton<ITelemetryInitializer, HttpDependenciesParsingTelemetryInitializer>();
                    services.AddSingleton<ITelemetryInitializer, WebJobsRoleEnvironmentTelemetryInitializer>();
                    services.AddSingleton<ITelemetryInitializer, WebJobsTelemetryInitializer>();
                    services.AddSingleton<ITelemetryInitializer, WebJobsSanitizingInitializer>();
                    services.AddSingleton<ITelemetryInitializer, ActivityTagsTelemetryIntitializer>();
                    services.AddSingleton<ITelemetryInitializer, MyTelemetryInitializer>();
                    services.AddSingleton<ITelemetryModule, QuickPulseTelemetryModule>();
                    services.AddSingleton<ITelemetryModule, DependencyTrackingTelemetryModule>(provider =>
                    {
                        var dependencyCollector = new DependencyTrackingTelemetryModule();
                        var excludedDomains = dependencyCollector.ExcludeComponentCorrelationHttpHeadersOnDomains;
                        excludedDomains.Add("core.windows.net");
                        excludedDomains.Add("core.chinacloudapi.cn");
                        excludedDomains.Add("core.cloudapi.de");
                        excludedDomains.Add("core.usgovcloudapi.net");
                        excludedDomains.Add("localhost");
                        excludedDomains.Add("127.0.0.1");

                        var includedActivities = dependencyCollector.IncludeDiagnosticSourceActivities;
                        includedActivities.Add("Microsoft.Azure.EventHubs");
                        includedActivities.Add("Microsoft.Azure.ServiceBus");

                        return dependencyCollector;
                    });

                    //heartbeat
                    //services.AddSingleton<IApplicationInsightDiagnosticListener, HostingDiagnosticListener>();

                    services.AddSingleton<TelemetryConfiguration>(provider =>
                    {
                        TelemetryConfiguration config = TelemetryConfiguration.CreateDefault();
                        config.InstrumentationKey = instrumentationKey;

                        ITelemetryChannel channel = new ServerTelemetryChannel();
                        ((ITelemetryModule) channel)?.Initialize(config);
                        config.TelemetryChannel = channel;

                        foreach (ITelemetryInitializer initializer in provider.GetServices<ITelemetryInitializer>())
                        {
                            config.TelemetryInitializers.Add(initializer);
                        }

                        foreach (ITelemetryModule module in provider.GetServices<ITelemetryModule>())
                        {
                            module.Initialize(config);
                        }

                        QuickPulseTelemetryModule quickPulseModule = (QuickPulseTelemetryModule)provider.GetServices<ITelemetryModule>().SingleOrDefault(m => m is QuickPulseTelemetryModule);
                        if (quickPulseModule != null)
                        {
                            config.TelemetryProcessorChainBuilder
                                .Use((next) =>
                                {
                                    QuickPulseTelemetryProcessor processor = new QuickPulseTelemetryProcessor(next);
                                    quickPulseModule.RegisterTelemetryProcessor(processor);
                                    return processor;
                                });
                        }

                        config.TelemetryProcessorChainBuilder.Use((next) => new FilteringTelemetryProcessor(filter.Filter, next));


                        if (samplingSettings != null)
                        {
                            config.TelemetryProcessorChainBuilder.Use((next) =>
                                new AdaptiveSamplingTelemetryProcessor(samplingSettings, null, next));
                        }

                        config.TelemetryProcessorChainBuilder.Build();

                        return config;
                    });
                    services.AddSingleton<TelemetryClient>(provider =>
                    {
                        TelemetryConfiguration configuration = provider.GetService<TelemetryConfiguration>();

                        TelemetryClient client = new TelemetryClient(configuration);

                        string assemblyVersion = GetAssemblyFileVersion(typeof(JobHost).Assembly);
                        client.Context.GetInternalContext().SdkVersion = $"webjobs: {assemblyVersion}";

                        return client;
                    });

                    services.AddSingleton<ILoggerProvider, ApplicationInsightsLoggerProvider>();
                });
            }

            return builder;
        }

        internal static string GetAssemblyFileVersion(Assembly assembly)
        {
            AssemblyFileVersionAttribute fileVersionAttr = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
            return fileVersionAttr?.Version ?? LoggingConstants.Unknown;
        }
    }

    class ActivityTagsTelemetryIntitializer : ITelemetryInitializer
    {
        private const string OperationContext = "MS_OperationContext";
        private const string DateTimeFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffK";
        private static readonly string[] SystemScopeKeys =
        {
            LogConstants.CategoryNameKey,
            LogConstants.LogLevelKey,
            LogConstants.OriginalFormatKey,
            ScopeKeys.Event,
            ScopeKeys.FunctionInvocationId,
            ScopeKeys.FunctionName,
            ScopeKeys.HostInstanceId,
            OperationContext
        };

        public void Initialize(ITelemetry telemetry)
        {
            var currentActivity = Activity.Current;
            if (currentActivity != null)
            {
                foreach (var tag in currentActivity.Tags)
                {
                    if (!telemetry.Context.Properties.ContainsKey(tag.Key))
                    {
                        telemetry.Context.Properties.Add(tag);
                    }
                }
            }

            if (/*!(telemetry is MetricTelemetry)TODO &&*/ telemetry is ISupportProperties propTelemetry)
            {
                var scopeProperties = DictionaryLoggerScope.GetMergedStateDictionary()
                    .Where(p => !SystemScopeKeys.Contains(p.Key, StringComparer.Ordinal));
                ApplyProperties(propTelemetry, scopeProperties, LogConstants.CustomPropertyPrefix);
            }
        }

        // Inserts properties into the telemetry's properties. Properly formats dates, removes nulls, applies prefix, etc.
        private static void ApplyProperties(ISupportProperties telemetry, IEnumerable<KeyValuePair<string, object>> values, string propertyPrefix = null)
        {
            foreach (var property in values)
            {
                string stringValue = null;

                // drop null properties
                if (property.Value == null)
                {
                    continue;
                }

                // Format dates
                Type propertyType = property.Value.GetType();
                if (propertyType == typeof(DateTime))
                {
                    stringValue = ((DateTime)property.Value).ToUniversalTime().ToString(DateTimeFormatString);
                }
                else if (propertyType == typeof(DateTimeOffset))
                {
                    stringValue = ((DateTimeOffset)property.Value).UtcDateTime.ToString(DateTimeFormatString);
                }
                else
                {
                    stringValue = property.Value.ToString();
                }

                telemetry.Properties.Add($"{propertyPrefix}{property.Key}", stringValue);
            }
        }
    }

    class MyTelemetryInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry is DependencyTelemetry dep && dep.Data != null)
            {
                Debug.WriteLine($"!!!!!! {dep.Data} {string.Join(", ", dep.Properties.Keys)}");
            }

            if (DictionaryLoggerScope.Current != null)
            {
                if (DictionaryLoggerScope.Current.State.ContainsKey(LogConstants.CategoryNameKey))
                {
                    telemetry.Context.Properties[LogConstants.CategoryNameKey] = DictionaryLoggerScope.Current.State[LogConstants.CategoryNameKey].ToString();
                }

                if (DictionaryLoggerScope.Current.State.ContainsKey(LogConstants.LogLevelKey))
                {
                    telemetry.Context.Properties[LogConstants.LogLevelKey] = DictionaryLoggerScope.Current.State[LogConstants.LogLevelKey].ToString();
                }
            }
        }
    }
}