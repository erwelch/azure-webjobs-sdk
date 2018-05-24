// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.Azure.WebJobs.Logging.ApplicationInsights
{
    internal class WebJobsTelemetryInitializer : ITelemetryInitializer
    {
        private const string ComputerNameKey = "COMPUTERNAME";
        private const string WebSiteInstanceIdKey = "WEBSITE_INSTANCE_ID";

        private static string _roleInstanceName = GetRoleInstanceName();

        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry == null)
            {
                return;
            }

            telemetry.Context.Cloud.RoleInstance = _roleInstanceName;

            telemetry.Context.Location.Ip = LoggingConstants.ZeroIpAddress;



            // Apply our special scope properties
            IDictionary<string, object> scopeProps = null;
            if (Activity.Current != null)
            {
                // TODO: multiple tags with the same name
                scopeProps = new Dictionary<string, object>();
                foreach (var tag in Activity.Current.Tags)
                {
                    if (!scopeProps.ContainsKey(tag.Key))
                    {
                        scopeProps.Add(tag.Key, tag.Value);
                    }
                }
            }
            else
            {
                scopeProps = DictionaryLoggerScope.GetMergedStateDictionary() ?? new Dictionary<string, object>();
            }


            //telemetry.Context.Operation.Id = scopeProps.GetValueOrDefault<string>(ScopeKeys.FunctionInvocationId);
            telemetry.Context.Operation.Name = scopeProps.GetValueOrDefault<string>(ScopeKeys.FunctionName);

            // Apply Category and LogLevel to all telemetry
            if (telemetry is ISupportProperties telemetryProps)
            {
                string category = scopeProps.GetValueOrDefault<string>(LogConstants.CategoryNameKey);
                if (category != null)
                {
                    telemetryProps.Properties[LogConstants.CategoryNameKey] = category;
                }

                object logLevel = scopeProps.GetValueOrDefault<object>(LogConstants.LogLevelKey);
                if (logLevel != null)
                {
                    telemetryProps.Properties[LogConstants.LogLevelKey] = logLevel.ToString();
                }
            }

            if (telemetry is RequestTelemetry requestTelemetry)
            {
                requestTelemetry.Name = telemetry.Context.Operation.Name;
                requestTelemetry.ResponseCode = "0";
                object succeeded = scopeProps.GetValueOrDefault<object>(LogConstants.SucceededKey);
                if (succeeded != null)
                {
                    requestTelemetry.Success = succeeded.ToString() == bool.TrueString;
                }
            }
        }

        private static string GetRoleInstanceName()
        {
            string instanceName = Environment.GetEnvironmentVariable(WebSiteInstanceIdKey);
            if (string.IsNullOrEmpty(instanceName))
            {
                instanceName = Environment.GetEnvironmentVariable(ComputerNameKey);
            }

            return instanceName;
        }
    }
}
