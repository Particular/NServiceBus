namespace NServiceBus.AcceptanceTests.Tx
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_receiving_with_native_multi_queue_transaction : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_send_outgoing_messages_if_receiving_transaction_is_rolled_back()
        {
            Requires.CrossQueueTransactionSupport();

            var context = await Scenario.Define<Context>(c => { c.FirstAttempt = true; })
                .WithEndpoint<Endpoint>(b => b.When(session => session.SendLocal(new MyMessage())))
                .Done(c => c.MessageHandled)
                .Run();

            Assert.IsFalse(context.HasFailed);
            Assert.IsTrue(context.MessageHandled);
        }

        public class Context : ScenarioContext
        {
            public bool FirstAttempt { get; set; }
            public bool MessageHandled { get; set; }
            public bool HasFailed { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    config.Recoverability().Immediate(immediate => immediate.NumberOfRetries(1));
                    config.ConfigureTransport()
                        .Transactions(TransportTransactionMode.SendsAtomicWithReceive);
                });
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public MyMessageHandler(Context context)
                {
                    testContext = context;
                }

                public async Task Handle(MyMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    if (testContext.FirstAttempt)
                    {
                        await context.SendLocal(new MessageHandledEvent
                        {
                            HasFailed = true
                        });
                        testContext.FirstAttempt = false;
                        throw new SimulatedException();
                    }

                    await context.SendLocal(new MessageHandledEvent());
                }

                Context testContext;
            }

            public class MessageHandledEventHandler : IHandleMessages<MessageHandledEvent>
            {
                public MessageHandledEventHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MessageHandledEvent message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.MessageHandled = true;
                    testContext.HasFailed |= message.HasFailed;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }


        public class MyMessage : ICommand
        {
        }


        public class MessageHandledEvent : IMessage
        {
            public bool HasFailed { get; set; }
        }
    }
}