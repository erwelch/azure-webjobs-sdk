// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;

namespace Microsoft.Azure.WebJobs.Logging.ApplicationInsights
{
    internal static class LoggingConstants
    {
        public const string ZeroIpAddress = "0.0.0.0";
        public const string Unknown = "[Unknown]";

        public const string W3CVersionTag = "w3c_version";
        public const string W3CTraceIdTag = "w3c_traceId";
        public const string W3CSpanIdTag = "w3c_spanId";
        public const string W3CSampledTag = "w3c_sampled";
        public const string W3CTraceStateTag = "w3c_tracestate";

        public static readonly string SdkVersion = $"webjobs: {GetAssemblyFileVersion(typeof(JobHost).Assembly)}";
        internal static string GetAssemblyFileVersion(Assembly assembly)
        {
            AssemblyFileVersionAttribute fileVersionAttr = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
            return fileVersionAttr?.Version ?? LoggingConstants.Unknown;
        }
    }
}
