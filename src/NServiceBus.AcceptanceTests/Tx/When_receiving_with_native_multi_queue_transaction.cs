namespace NServiceBus.AcceptanceTests.Tx
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_receiving_with_native_multi_queue_transaction : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_send_outgoing_messages_if_receiving_transaction_is_rolled_back()
        {
            await Scenario.Define<Context>(c => { c.FirstAttempt = true; })
                 .WithEndpoint<Endpoint>(b => b.When(bus => bus.SendLocal(new MyMessage())))
                 .Done(c => c.MessageHandled)
                 .Repeat(r => r.For<AllNativeMultiQueueTransactionTransports>())
                 .Should(c =>
                 {
                     Assert.IsFalse(c.HasFailed);
                     Assert.IsTrue(c.MessageHandled);
                 })
                 .Run();
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
                    config.EnableFeature<FirstLevelRetries>();
                    config.UseTransport(context.GetTransportType())
                            .Transactions(TransportTransactionMode.ReceiveOnly);
                });
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context TestContext { get; set; }

                public async Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    if (TestContext.FirstAttempt)
                    {
                        await context.SendLocal(new MessageHandledEvent
                        {
                            HasFailed = true
                        });
                        TestContext.FirstAttempt = false;
                        throw new SimulatedException();
                    }

                    await context.SendLocal(new MessageHandledEvent());
                }
            }

            public class MessageHandledEventHandler : IHandleMessages<MessageHandledEvent>
            {
                public Context TestContext { get; set; }

                public Task Handle(MessageHandledEvent message, IMessageHandlerContext context)
                {
                    TestContext.MessageHandled = true;
                    TestContext.HasFailed |= message.HasFailed;
                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class MyMessage : ICommand
        {
        }

        [Serializable]
        public class MessageHandledEvent : IMessage
        {
            public bool HasFailed { get; set; }
        }
    }
}