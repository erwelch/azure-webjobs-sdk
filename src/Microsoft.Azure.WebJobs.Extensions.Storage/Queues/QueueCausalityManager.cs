// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Host.Queues
{
    /// <summary>
    /// Tracks causality via JSON formatted queue message content. 
    /// Adds an extra field to the JSON object for the parent guid name.
    /// </summary>
    /// <remarks>
    /// Important that this class can interoperate with external queue messages, 
    /// so be resilient to a missing guid marker. 
    /// Can we switch to some auxiliary table? Beware, CloudQueueMessage. 
    /// Id is not filled out until after the message is queued, 
    /// but then there's a race between updating the aux storage and another function picking up the message.
    /// </remarks>
    internal static class QueueCausalityManager
    {
        private const string ParentGuidFieldName = "$AzureWebJobsParentId";
        private const string TraceparentFieldName = "$AzureWebJobsTraceparent";
        private const string TracestateFieldName = "$AzureWebJobsTracestate";

        public static void SetOwner(Guid functionOwner, JObject token)
        {
            if (token == null)
            {
                throw new ArgumentNullException("token");
            }

            if (!Guid.Equals(Guid.Empty, functionOwner))
            {
                token[ParentGuidFieldName] = functionOwner.ToString();
            }
        }

        public static void SetTraceContext(string traceparent, string tracestate, JObject token)
        {
            if (token == null)
            {
                throw new ArgumentNullException("token");
            }

            if (!string.IsNullOrEmpty(traceparent))
            {
                token[TraceparentFieldName] = traceparent;

                if (!string.IsNullOrEmpty(tracestate))
                {
                    token[TracestateFieldName] = tracestate;
                }
            }
        }

        [DebuggerNonUserCode]
        public static Guid? GetOwner(CloudQueueMessage msg)
        {
            string text = msg.TryGetAsString();

            if (text == null)
            {
                return null;
            }

            IDictionary<string, JToken> json;
            try
            {
                json = JsonSerialization.ParseJObject(text);
            }
            catch (Exception)
            {
                return null;
            }

            if (json == null || !json.ContainsKey(ParentGuidFieldName) || json[ParentGuidFieldName].Type != JTokenType.String)
            {
                return null;
            }

            string val = (string)json[ParentGuidFieldName];

            Guid guid;
            if (Guid.TryParse(val, out guid))
            {
                return guid;
            }
            return null;
        }

        [DebuggerNonUserCode]
        public static (string traceparent, string tracestate) GetTraceContext(CloudQueueMessage msg)
        {
            string text = msg.TryGetAsString();

            if (text == null)
            {
                return (null, null);
            }

            IDictionary<string, JToken> json;
            try
            {
                json = JsonSerialization.ParseJObject(text);
            }
            catch (Exception)
            {
                return (null, null);
            }

            if (json == null || !json.ContainsKey(TraceparentFieldName) || json[TraceparentFieldName].Type != JTokenType.String)
            {
                return (null, null);
            }

            string tracestate = null;
            if (json.ContainsKey(TracestateFieldName) && json[TracestateFieldName].Type == JTokenType.String)
            {
                tracestate = (string) json[TracestateFieldName];
            }

            return ((string) json[TraceparentFieldName], tracestate);
        }
    }
}
