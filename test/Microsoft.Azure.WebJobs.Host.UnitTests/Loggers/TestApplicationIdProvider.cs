// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.Azure.WebJobs.Host.UnitTests.Loggers
{
    class TestApplicationIdProvider : IApplicationIdProvider
    {
        private const string _mockInstrumentationKey = "some-ikey";
        private const string _mockApplicationId = "some-appId";

        public bool TryGetApplicationId(string instrumentationKey, out string applicationId)
        {
            if (instrumentationKey == _mockInstrumentationKey)
            {
                applicationId = _mockApplicationId;
                return true;
            }

            applicationId = null;
            return false;
        }
    }
}
