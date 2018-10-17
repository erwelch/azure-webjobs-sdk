// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.W3C;
using Microsoft.Azure.WebJobs.Host.TestCommon;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Xunit;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Microsoft.Azure.WebJobs.Host.EndToEndTests.ApplicationInsights
{
    public class QueueProto
    {
        private const string TestArtifactPrefix = "e2etestsai";
        private const string TriggerQueueNamePattern = TestArtifactPrefix + "trigger%rnd%";
        private const string _mockApplicationInsightsKey = "insert Ikey here";

        private static readonly AutoResetEvent _functionWaitHandle = new AutoResetEvent(false);

        private static string _triggerQueueName;

        private RandomNameResolver _resolver;
        private static CloudQueue _triggerQueue;
        private CloudQueueClient _queueClient;
        private static TelemetryConfiguration configuration;
        private static TelemetryClient client;

        [Fact]
        public async Task QueueTriggerCallsAreReported()
        {
            using (var host = ConfigureHost(LogLevel.Information))
            {
                await host.StartAsync();

                await host.GetJobHost()
                    .CallAsync(typeof(QueueProto).GetMethod(nameof(QueueOut)));

                _functionWaitHandle.WaitOne();
                // let host run for a while to write output queue message
                await Task.Delay(10000);

                await host.StopAsync();
            }
        }

        // this is not correct because dependency is reported before conversion happens
        // and queue causation manager is called after dependency scope ends
        // so we'll construct message ourselves

        // alternatives are:
        // 1. In the ConvertPocoToCloudQueueMessage, create a new Activity and send events through DiagnosticSource,
        //    so AI will track telemetry
        // 2. Make durable functions track telemtery manually and return CloudQueueMessage
        //    see example in QueueOut below
/*        [NoAutomaticTrigger]
        public static void QueueOut(
            [Queue(TriggerQueueNamePattern)] out MyObject output, ILogger logger)
        {
            var dependency = new DependencyTelemetry("Azure queue", _triggerQueue.StorageUri.PrimaryUri.AbsoluteUri, "Enqueue " + _triggerQueueName, null);
            using (client.StartOperation(dependency))
            {
                output = new MyObject
                {
                    msg = "test",
                    timsetamp = DateTime.UtcNow.ToString("o")
                };
            }
        }*/

        [NoAutomaticTrigger]
        public static void QueueOut(
            [Queue(TriggerQueueNamePattern)] out CloudQueueMessage message, ILogger logger)
        {
            var dependency = new DependencyTelemetry("Azure queue", _triggerQueue.StorageUri.PrimaryUri.AbsoluteUri, "Enqueue " + _triggerQueueName, null);
            using (client.StartOperation(dependency))
            {
                Activity.Current.UpdateContextOnActivity();
                string messageJson = "{\"msg\":\"123\", \"$AzureWebJobsTraceparent\":\"" +
                                     Activity.Current.GetTraceparent() + "\"}";

                message = new CloudQueueMessage(messageJson);
            }
        }
        public async Task QueueTrigger(
            [QueueTrigger(TriggerQueueNamePattern)] MyObject input)
        {
            using (HttpClient hc = new HttpClient())
            {
                await hc.GetAsync("http://microsoft.com");
            }

            _functionWaitHandle.Set();
        }

        public IHost ConfigureHost(LogLevel logLevel)
        {
            _resolver = new RandomNameResolver();

            IHost host = new HostBuilder()
                .ConfigureDefaultTestHost<QueueProto>(b =>
                {
                    b.AddAzureStorage();
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton<INameResolver>(_resolver);
                    services.Configure<FunctionResultAggregatorOptions>(o =>
                    {
                        o.IsEnabled = false;
                    });
                })
                .ConfigureLogging(b =>
                {
                    b.SetMinimumLevel(logLevel);
                    b.AddApplicationInsights(o => o.InstrumentationKey = _mockApplicationInsightsKey);
                })
                .Build();

            StorageAccountProvider provider = host.Services.GetService<StorageAccountProvider>();
            CloudStorageAccount storageAccount = provider.GetHost().SdkObject;
            _queueClient = storageAccount.CreateCloudQueueClient();
            _triggerQueueName = _resolver.ResolveInString(TriggerQueueNamePattern);
            _triggerQueue = _queueClient.GetQueueReference(_triggerQueueName);
            _triggerQueue.CreateIfNotExistsAsync().Wait();

            configuration = host.Services.GetService<TelemetryConfiguration>();
            client = host.Services.GetService<TelemetryClient>();
            return host;
        }

        public class MyObject
        {
            public string msg { get; set; }
            public string timsetamp { get; set; }
        }
    }
}
