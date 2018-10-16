// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.ApplicationInsights.SnapshotCollector;
using Microsoft.ApplicationInsights.WindowsServer.Channel.Implementation;

namespace Microsoft.Azure.WebJobs.Logging.ApplicationInsights
{
    public class ApplicationInsightsLoggerOptions
    {
        public const string DefaultSdkVersionPrefix = "webjobs";

        public string InstrumentationKey { get; set; }

        public SamplingPercentageEstimatorSettings SamplingSettings { get; set; }

        public SnapshotCollectorConfiguration SnapshotConfiguration { get; set; }

        public string QuickPulseAuthenticationApiKey { get; set; }

        public bool DisableW3CDistributedTracing { get; set; } = false;

        public bool EnableResponseHeaderInjection { get; set; } = false;

        public string SdkVersionPrefix { get; set; } = DefaultSdkVersionPrefix;
    }
}
