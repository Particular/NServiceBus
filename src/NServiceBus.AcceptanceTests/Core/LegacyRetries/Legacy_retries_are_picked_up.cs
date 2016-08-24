namespace NServiceBus.AcceptanceTests.Core.LegacyRetries
{
    using System;
    using System.Linq;
    using System.Messaging;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using Support;

    public class Legacy_retries_are_picked_up : NServiceBusAcceptanceTest
    {
        [Test]
        public void test()
        {
            //setup => create queue & insert messages to be pickedup
            var queueName = "legacyretriesarepickedup.legacyendpoint.retries";
            var path = $@"{RuntimeEnvironment.MachineName}\private$\{queueName}";
            if (MessageQueue.Exists(path))
            {
                MessageQueue.Delete(path);
            }
            MessageQueue.Create(path, true);
            var retriesQueues = MessageQueue.GetPrivateQueuesByMachine(".");
            try
            {
                Scenario.Define<MyContext>()
                    .WithEndpoint<RetryEndpoint>()
                    .Done(c => c.NumberOfPickedUpMessages == 3)
                    .Run(TimeSpan.FromSeconds(10));
            }
            catch (Exception exc)
            {
                var res = exc;
            }
            var retriesQueue = MessageQueue.GetPrivateQueuesByMachine(".")
                .Single(q => q.QueueName.Equals($@"private$\{queueName}"));

            Assert.IsNull(retriesQueue.Peek());
        }


        class MyContext : ScenarioContext
        {
            public int NumberOfPickedUpMessages { get; set; }

        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.UseTransport<MsmqTransport>())
                    .AddMapping<LegacyRetryMessage>(typeof(RetryEndpoint));
            }

            class LegacyRetriesMessages : IHandleMessages<LegacyRetryMessage>
            {
                public MyContext TestMyContext { get; set; }

                public Task Handle(LegacyRetryMessage legacyRetryMessage, IMessageHandlerContext context)
                {
                    TestMyContext.NumberOfPickedUpMessages++;

                    return Task.FromResult(0);
                }
            }
        }
        
        public class LegacyRetryMessage : IMessage
        {
        }
    }
}
