// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Host.Executors
{
    /// <summary>
    /// Represents the input values for a triggered function invocation.
    /// </summary>
    public class TriggeredFunctionData
    {
        /// <summary>
        /// The parent ID for the triggered function invocation.
        /// </summary>
        [Obsolete("Use ParentActivity instead.")]
        public Guid? ParentId { get; set; }

        /// <summary>
        /// The trace context for correlation.
        /// </summary>
        public Activity ParentActivity { get; set; }

        /// <summary>
        /// The trigger value for a specific triggered function invocation.
        /// </summary>
        public object TriggerValue { get; set; }

        /// <summary>
        /// Optional handler function for processing the invocation.
        /// </summary>
        [Obsolete("Not ready for public consumption.")]
        public Func<Func<Task>, Task> InvokeHandler { get; set; }
    }
}
