namespace NServiceBus.AcceptanceTests.Tx
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class When_receiving_with_native_multi_queue_transaction : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_not_send_outgoing_messages_if_receiving_transaction_is_rolled_back()
        {
            var context = new Context
            {
                FirstAttempt = true
            };
            Scenario.Define(context)
                .WithEndpoint<Endpoint>(b => b.Given(bus => bus.SendLocal(new MyMessage())))
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
                EndpointSetup<DefaultServer>(c => c.Transactions()
                    .DisableDistributedTransactions());
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }
                public IBus Bus { get; set; }

                public void Handle(MyMessage evnt)
                {
                    if (Context.FirstAttempt)
                    {
                        Bus.SendLocal(new MessageHandledEvent
                        {
                            HasFailed = true
                        });
                        Context.FirstAttempt = false;
                        throw new Exception();
                    }
                    Bus.SendLocal(new MessageHandledEvent());
                }
            }

            public class MessageHandledEventHandler : IHandleMessages<MessageHandledEvent>
            {
                public Context Context { get; set; }
                public IBus Bus { get; set; }

                public void Handle(MessageHandledEvent evnt)
                {
                    Context.MessageHandled = true;
                    Context.HasFailed |= evnt.HasFailed;
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