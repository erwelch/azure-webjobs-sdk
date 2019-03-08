// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.Azure.WebJobs.Logging.ApplicationInsights
{
    /// <summary>
    /// Noop telemetry module that is added instead of another one, which is disabled by settings.
    /// </summary>
    /// <typeparam name="T">Type of the module substituted by this module.
    /// Noop module is added to DI as singleton and generic type makes it unique.</typeparam>
    internal class NullTelemetryModule<T> : ITelemetryModule
    {
        public void Initialize(TelemetryConfiguration configuration)
        {
        }
    }
}
