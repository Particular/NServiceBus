namespace NServiceBus.AcceptanceTests.Timeouts
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Transports;
    using NUnit.Framework;

    public class When_dispatching_deferred_message_fails_without_dtc : NServiceBusAcceptanceTest
    {
        [Test]
        public void Message_should_be_received()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<TimeoutHandlingEndpoint>(b => b.Given(bus =>
                    {
                        bus.Defer(TimeSpan.FromSeconds(3), new MyMessage());
                    }))
                    .Done(c => c.MessageReceived)
                    .Run();

            Assert.IsTrue(context.SendingMessageFailedOnce);
            Assert.IsTrue(context.MessageReceived);
        }

        public class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }

            public bool SendingMessageFailedOnce { get; set; }
        }

        public class TimeoutHandlingEndpoint : EndpointConfigurationBuilder
        {
            public TimeoutHandlingEndpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    Configure.Transactions.Advanced(s => s.DisableDistributedTransactions());
                }).AllowExceptions();
            }

            public class DelayedMessageHandler : IHandleMessages<MyMessage>
            {
                Context context;

                public DelayedMessageHandler(Context context)
                {
                    this.context = context;
                }

                public void Handle(MyMessage message)
                {
                    context.MessageReceived = true;
                }
            }

            public class EndpointConfiguration : IWantToRunWhenConfigurationIsComplete
            {
                Context context;
                ISendMessages originalMessageSender;

                public EndpointConfiguration(Context context, ISendMessages originalMessageSender)
                {
                    this.context = context;
                    this.originalMessageSender = originalMessageSender;
                }

                public void Run()
                {
                    Configure.Component(b => new SenderWrapper(originalMessageSender, context), DependencyLifecycle.SingleInstance);
                }
            }

            class SenderWrapper : ISendMessages
            {
                ISendMessages wrappedSender;
                Context context;

                public SenderWrapper(ISendMessages wrappedSender, Context context)
                {
                    this.wrappedSender = wrappedSender;
                    this.context = context;
                }

                public void Send(TransportMessage message, Address address)
                {
                    string realtedTimeoutId;
                    if (message.Headers.TryGetValue("NServiceBus.RelatedToTimeoutId", out realtedTimeoutId))
                    {
                        // dispatched message by timeout behavior
                        // fail first attempt:
                        if (!context.SendingMessageFailedOnce)
                        {
                            context.SendingMessageFailedOnce = true;
                            throw new Exception("simulated exception");
                        }
                    }

                    wrappedSender.Send(message, address);
                }
            }
        }

        [Serializable]
        public class MyMessage : IMessage
        {
        }
    }
}