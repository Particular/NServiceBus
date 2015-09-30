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
            var context = new Context();
            Scenario.Define(context)
                .WithEndpoint<TimeoutHandlingEndpoint>(b => b.Given((bus, c) =>
                {
                    bus.Defer(TimeSpan.FromSeconds(3), new MyMessage());
                }))
                .AllowExceptions()
                .Done(c => c.MessageReceivedByHandler)
                .Run();

            Assert.IsTrue(context.SendingMessageFailedOnce, "Sending message attempt should fail once.");
            Assert.IsTrue(context.MessageReceivedByHandler, "Message should be sent and received by handler on second attempt.");
        }

        public class Context : ScenarioContext
        {
            public bool MessageReceivedByHandler { get; set; }
            public bool SendingMessageFailedOnce { get; set; }
        }

        public class TimeoutHandlingEndpoint : EndpointConfigurationBuilder
        {
            public TimeoutHandlingEndpoint()
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
                    context.MessageReceivedByHandler = true;
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
                    var ctx = EndpointConfiguration.builder.Build<Context>();
                    context.Container.ConfigureComponent(() => new SenderWrapper(originalDispatcher, ctx), DependencyLifecycle.SingleInstance);
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

                public void Send(TransportMessage message, SendOptions sendOptions)
                {
                    string relatedTimeoutId;
                    if (message.Headers.TryGetValue("NServiceBus.RelatedToTimeoutId", out relatedTimeoutId) && !context.SendingMessageFailedOnce)
                    {
                        context.SendingMessageFailedOnce = true;
                        throw new Exception("simulated exception");
                    }

                    wrappedSender.Send(message, sendOptions);
                }
            }
        }

        [Serializable]
        public class MyMessage : IMessage { }
    }
}
