// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.Azure.WebJobs.Host.Protocols;

namespace Microsoft.Azure.WebJobs.Host.Executors
{
    public interface IFunctionInstance
    {
        Guid Id { get; }

        [Obsolete("Use ParentActivity instead.")]
        Guid? ParentId { get; }

        Activity ParentActivity { get; }

        ExecutionReason Reason { get; }

        IBindingSource BindingSource { get; }

        IFunctionInvoker Invoker { get; }

        FunctionDescriptor FunctionDescriptor { get; }
    }
}
