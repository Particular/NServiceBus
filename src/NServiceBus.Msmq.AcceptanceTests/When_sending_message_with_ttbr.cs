namespace NServiceBus.Transport.Msmq.AcceptanceTests
{
    using System;
    using System.Messaging;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_sending_message_with_ttbr : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_set_dlq_flag_by_default()
        {
            DeleteSpyQueue();
            MessageQueue.Create(sendSpyQueue, true);
            try
            {
                await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When(async (session, c) =>
                {
                    await session.Send(sendSpyEndpoint, new MyMessage());

                    c.WasCalled = true;
                }))
                .Done(c => c.WasCalled)
                .Run();

                using (var queue = new MessageQueue(sendSpyQueue))
                {
                    using (var message = queue.Receive(TimeSpan.FromSeconds(5)))
                    {
                        Assert.False(message?.UseDeadLetterQueue);
                    }
                }
            }
            finally
            {
                DeleteSpyQueue();
            }
        }

        static void DeleteSpyQueue()
        {
            if (MessageQueue.Exists(sendSpyQueue))
            {
                MessageQueue.Delete(sendSpyQueue);
            }
        }

        static string sendSpyEndpoint = "dlqForTTBRSpy";
        static string sendSpyQueue = $@".\private$\{sendSpyEndpoint}";

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseTransport<MsmqTransport>();
                });
            }
        }

        [TimeToBeReceived("00:01:00")]
        public class MyMessage : IMessage
        {
        }
    }
}