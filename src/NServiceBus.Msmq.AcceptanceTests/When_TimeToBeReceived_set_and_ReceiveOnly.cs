namespace NServiceBus.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NUnit.Framework;
    using EndpointTemplates;

    public class When_TimeToBeReceived_set_and_ReceiveOnly : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_throw()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<TtbrEndpoint>(e => e
                        .When(s => s.SendLocal(new StartMessage())))
                    .Done(c => c.ReceivedTtbrMessage)
                    .Run();

            Assert.True(context.ReceivedTtbrMessage);
            Assert.AreEqual("00:00:15", context.TimeToBeReceived);
        }

        class Context : ScenarioContext
        {
            public bool ReceivedTtbrMessage { get; set; }
            public string TimeToBeReceived { get; set; }
        }

        class TtbrEndpoint : EndpointConfigurationBuilder
        {
            public TtbrEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c
                    .UseTransport<MsmqTransport>()
                        .Transactions(TransportTransactionMode.ReceiveOnly));
            }

            class StartMessageHandler : IHandleMessages<StartMessage>
            {
                public Task Handle(StartMessage message, IMessageHandlerContext context)
                {
                    return context.SendLocal(new TtbrMessage());
                }
            }

            class TtbrMessageHandler : IHandleMessages<TtbrMessage>
            {
                Context testContext;

                public TtbrMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(TtbrMessage ttbrMessage, IMessageHandlerContext context)
                {
                    testContext.ReceivedTtbrMessage = true;
                    testContext.TimeToBeReceived = context.MessageHeaders[Headers.TimeToBeReceived];
                    return Task.CompletedTask;
                }
            }
        }

        class StartMessage : ICommand
        {
        }

        [TimeToBeReceived("00:00:15")]
        class TtbrMessage : ICommand
        {
        }
    }
}