// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.WindowsServer.Channel.Implementation;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Azure.WebJobs.Logging.ApplicationInsights;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Extensions for adding the <see cref="ApplicationInsightsLoggerProvider"/> to an <see cref="ILoggerFactory"/>. 
    /// </summary>
    public static class ApplicationInsightsLoggerExtensions
    {
        /// <summary>
        /// Registers an <see cref="ApplicationInsightsLoggerProvider"/> with an <see cref="ILoggerFactory"/>.
        /// </summary>
        /// <param name="loggerFactory">The factory.</param>        
        /// <param name="telemetryClientFactory">The factory to use when creating the <see cref="TelemetryClient"/> </param>
        /// <returns>A <see cref="ILoggerFactory"/> for chaining additional operations.</returns>
        public static ILoggerFactory AddApplicationInsightsLogger(
            this ILoggerFactory loggerFactory,
            TelemetryClient telemetryClient)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            // Note: LoggerFactory calls Dispose() on all registered providers.
            loggerFactory.AddProvider(new ApplicationInsightsLoggerProvider(telemetryClient));

            return loggerFactory;
        }

        public static ILoggingBuilder AddApplicationInsightsLogger(this ILoggingBuilder builder)
        {
            var telemetryClient = (TelemetryClient)builder.Services.SingleOrDefault(s => s.ServiceType == typeof(TelemetryClient))?.ImplementationInstance;
            if (telemetryClient != null)
            {
                return builder.AddProvider(new ApplicationInsightsLoggerProvider(telemetryClient));
            }

            return builder;
        }
    }
}