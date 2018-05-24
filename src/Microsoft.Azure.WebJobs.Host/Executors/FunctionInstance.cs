// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.Azure.WebJobs.Host.Protocols;

namespace Microsoft.Azure.WebJobs.Host.Executors
{
    internal class FunctionInstance : IFunctionInstance
    {
        private readonly Guid _id;
        private readonly Activity _parentActivity;
        private readonly ExecutionReason _reason;
        private readonly IBindingSource _bindingSource;
        private readonly IFunctionInvoker _invoker;
        private readonly FunctionDescriptor _functionDescriptor;

        public FunctionInstance(Guid id, Activity parentActivity, ExecutionReason reason, IBindingSource bindingSource,
            IFunctionInvoker invoker, FunctionDescriptor functionDescriptor)
        {
            _id = id;
            _parentActivity = parentActivity;
            _reason = reason;
            _bindingSource = bindingSource;
            _invoker = invoker;
            _functionDescriptor = functionDescriptor;
        }

        public Guid Id
        {
            get { return _id; }
        }

        [Obsolete("Use ParentActivity instead.")]
        public Guid? ParentId { get; } = null;

        public Activity ParentActivity
        {
            get { return _parentActivity; }
        }

        public ExecutionReason Reason
        {
            get { return _reason; }
        }

        public IBindingSource BindingSource
        {
            get { return _bindingSource; }
        }

        public IFunctionInvoker Invoker
        {
            get { return _invoker; }
        }

        public FunctionDescriptor FunctionDescriptor
        {
            get { return _functionDescriptor; }
        }
    }
}
