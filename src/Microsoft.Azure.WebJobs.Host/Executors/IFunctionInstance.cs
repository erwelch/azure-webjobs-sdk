// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Host.Protocols;

namespace Microsoft.Azure.WebJobs.Host.Executors
{
    public interface IFunctionInstance
    {
        Guid Id { get; }

        IDictionary<string, string> TriggerDetails { get; }

        Guid? ParentId { get; }
        string Traceparent { get; }
        string Tracestate { get; }

        ExecutionReason Reason { get; }

        IBindingSource BindingSource { get; }

        IFunctionInvoker Invoker { get; }

        FunctionDescriptor FunctionDescriptor { get; }
    }
}
