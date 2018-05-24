// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SampleHost.Filters;
using SampleHost.Models;

namespace SampleHost
{
    [ErrorHandler]
    public static class Functions
    {
        public static HttpClient hc = new HttpClient();
        [Singleton]
        public static async Task BlobTrigger(
            [BlobTrigger("test")] string blob, ILogger logger)
        {
            await hc.GetAsync("http://microsoft.com");
            logger.LogInformation("Processed blob: " + blob);
        }

        [FunctionName("ServiceBusTrigger")]
        //[return: Blob("output-container/{id}")]
        public static void ProcessQueueMessage(
            [ServiceBusTrigger("test")] string message,
            [Blob("test/aaa.md", FileAccess.Read)] Stream myBlob,
            [Blob("orders/{MessageId}")] out string orderBlob,
            TextWriter logger)
        {
            using (StreamReader sr = new StreamReader(myBlob))
            {
                orderBlob = string.Format("{{ \"id\": \"{0}\" }}", sr.ReadToEnd());
            }
            logger.WriteLine($"C# script processed queue message. Item={orderBlob}");

        }

        public static void BlobPoisonBlobHandler(
            [QueueTrigger("webjobs-blobtrigger-poison")] JObject blobInfo, ILogger logger)
        {
            string container = (string)blobInfo["ContainerName"];
            string blobName = (string)blobInfo["BlobName"];

            logger.LogInformation($"Poison blob: {container}/{blobName}");
        }

        [WorkItemValidator]
        public static void ProcessWorkItem(
            [QueueTrigger("test")] WorkItem workItem, ILogger logger)
        {
            logger.LogInformation($"Processed work item {workItem.ID}");
        }
    }
}
