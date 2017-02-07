namespace NServiceBus.Transport.Msmq.AcceptanceTests
{
    using System;
    using System.Messaging;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_requesting_dlq_for_ttbr_messages : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_set_dlq_flag_on_message()
        {
            DeleteSpyQueue();
            MessageQueue.Create(sendSpyQueue, true);
            try
            {
                await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When(async (session, c) =>
                {
                    await session.Send("dlqOptinSpy", new MyMessage());

                    c.WasCalled = true;
                }))
                .Done(c => c.WasCalled)
                .Run();

                using (var queue = new MessageQueue(sendSpyQueue))
                {
                    using (var message = queue.Receive(TimeSpan.FromSeconds(5)))
                    {
                        Assert.True(message?.UseDeadLetterQueue);
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

        const string sendSpyQueue = @".\private$\dlqOptinSpy";

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
                    c.UseTransport<MsmqTransport>()
                        .UseDeadLetterQueueForMessagesWithTimeToReachQueue();
                });
            }
        }

        [TimeToBeReceived("00:01:00")]
        public class MyMessage : ICommand
        {
            public Guid Id { get; set; }
        }
    }
}