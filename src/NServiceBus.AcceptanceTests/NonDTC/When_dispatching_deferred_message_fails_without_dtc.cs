namespace NServiceBus.AcceptanceTests.NonDTC
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;
    using NUnit.Framework;

    public class When_dispatching_deferred_message_fails_without_dtc : NServiceBusAcceptanceTest
    {
        [Test]
        public void Message_should_be_received()
        {
            var delay = TimeSpan.FromSeconds(3);
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<Endpoint>(b => b.Given((bus, c) =>
                    {
                       bus.Defer(delay, new MyMessage());
                    }))
                    .AllowExceptions()
                    .Done(c => c.MessageReceived)
                    .Run(TimeSpan.FromSeconds(70));

            Assert.IsTrue(context.MessageReceived);
        }

        public class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.EnableFeature<TimeoutManager>();
                    config.Transactions().DisableDistributedTransactions();
                });
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

            public class EndpointConfiguration : IWantToRunBeforeConfigurationIsFinalized
            {
                public static IBuilder builder;

                public void Run(Configure config)
                {
                    builder = config.Builder;
                }
            }

            public class DispatcherInterceptor : Feature
            {
                public DispatcherInterceptor()
                {
                    EnableByDefault();
                    DependsOn<MsmqTransportConfigurator>();
                }

                protected override void Setup(FeatureConfigurationContext context)
                {
                    var originalDispatcher = EndpointConfiguration.builder.Build<ISendMessages>();
                    context.Container.ConfigureComponent(() => new SenderWrapper(originalDispatcher), DependencyLifecycle.SingleInstance);
                }
            }

            class SenderWrapper : ISendMessages
            {
                ISendMessages wrappedSender;
                bool failMessage = true;

                public SenderWrapper(ISendMessages wrappedDispatcher)
                {
                    this.wrappedSender = wrappedDispatcher;
                }

                public void Send(TransportMessage message, SendOptions sendOptions)
                {
                    string realtedTimeoutId;
                    if (message.Headers.TryGetValue("NServiceBus.RelatedToTimeoutId", out realtedTimeoutId))
                    {
                        // dispatched message by timeout behavior
                        // fail first attempt:
                        if (failMessage)
                        {
                            failMessage = false;
                            throw new Exception("simulated exception");
                        }
                    }

                    wrappedSender.Send(message, sendOptions);
                }
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}
