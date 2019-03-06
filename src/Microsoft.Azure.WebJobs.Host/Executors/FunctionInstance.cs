// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Host.Protocols;

namespace Microsoft.Azure.WebJobs.Host.Executors
{
    internal class FunctionInstance : IFunctionInstance
    {
        private readonly Guid _id;
        private readonly IDictionary<string, string> _triggerDetails;
        private readonly Guid? _parentId;
        private readonly string _traceparent;
        private readonly string _tracestate;
        private readonly ExecutionReason _reason;
        private readonly IBindingSource _bindingSource;
        private readonly IFunctionInvoker _invoker;
        private readonly FunctionDescriptor _functionDescriptor;

        public FunctionInstance(Guid id, IDictionary<string, string> triggerDetails, Guid? parentId, string traceparent, string tracestate, ExecutionReason reason, IBindingSource bindingSource,
            IFunctionInvoker invoker, FunctionDescriptor functionDescriptor)
        {
            _id = id;
            _triggerDetails = triggerDetails;
            _parentId = parentId;
            _traceparent = traceparent;
            _tracestate = tracestate;
            _reason = reason;
            _bindingSource = bindingSource;
            _invoker = invoker;
            _functionDescriptor = functionDescriptor;
        }

        public Guid Id
        {
            get { return _id; }
        }

        public IDictionary<string, string> TriggerDetails
        {
            get { return _triggerDetails; }
        }

        public Guid? ParentId
        {
            get { return _parentId; }
        }

        public string Traceparent
        {
            get { return _traceparent; }
        }

        public string Tracestate
        {
            get { return _tracestate; }
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
