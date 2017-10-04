namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Persistence;
    using NUnit.Framework;

    public class When_handling_current_message_later : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_throw_exception_when_outbox_enabled()
        {
            var ctx = await Scenario.Define<Context>()
                .WithEndpoint<EndpointHandlingMessageLater>(c => c
                    .When(s => s.SendLocal(new MessageToHandleLater())))
                .Done(c => c.Exception != null)
                .Run(TimeSpan.FromSeconds(30));

            StringAssert.Contains("HandleCurrentMessageLater cannot be used in conjunction with the Outbox.", ctx.Exception.Message);
        }

        class Context : ScenarioContext
        {
            public InvalidOperationException Exception { get; set; }
        }

        class EndpointHandlingMessageLater : EndpointConfigurationBuilder
        {
            public EndpointHandlingMessageLater()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UsePersistence<InMemoryPersistence, StorageType.Outbox>();
                    c.EnableOutbox();
                });
            }

            public class MessageHandler : IHandleMessages<MessageToHandleLater>
            {
                Context testContext;

                public MessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public async Task Handle(MessageToHandleLater message, IMessageHandlerContext context)
                {
                    try
                    {
                        await context.HandleCurrentMessageLater();
                    }
                    catch (InvalidOperationException e)
                    {
                        testContext.Exception = e;
                    }
                }
            }
        }

        public class MessageToHandleLater : IMessage
        {
        }
    }
}