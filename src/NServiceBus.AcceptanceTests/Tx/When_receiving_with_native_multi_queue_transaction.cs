namespace NServiceBus.AcceptanceTests.Tx
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class When_receiving_with_native_multi_queue_transaction : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_send_outgoing_messages_if_receiving_transaction_is_rolled_back()
        {
            await Scenario.Define<Context>(c => { c.FirstAttempt = true; })
                 .WithEndpoint<Endpoint>(b => b.When(bus => bus.SendLocalAsync(new MyMessage())))
                 .Done(c => c.MessageHandled)
                 .AllowSimulatedExceptions()
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

                public async Task Handle(MyMessage @event)
                {
                    if (Context.FirstAttempt)
                    {
                        await Bus.SendLocalAsync(new MessageHandledEvent
                        {
                            HasFailed = true
                        });
                        Context.FirstAttempt = false;
                        throw new SimulatedException();
                    }

                    await Bus.SendLocalAsync(new MessageHandledEvent());
                }
            }

            public class MessageHandledEventHandler : IHandleMessages<MessageHandledEvent>
            {
                public Context Context { get; set; }
                public IBus Bus { get; set; }

                public Task Handle(MessageHandledEvent @event)
                {
                    Context.MessageHandled = true;
                    Context.HasFailed |= @event.HasFailed;
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