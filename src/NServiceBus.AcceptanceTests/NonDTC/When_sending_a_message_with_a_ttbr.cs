namespace NServiceBus.AcceptanceTests.NonDTC
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Features;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;
    using NUnit.Framework;

    public class When_sending_a_message_with_a_ttbr : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_honor_it()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<Endpoint>(b => b.Given((bus, c) => bus.SendLocal(new StartMessage())))
                .Done(c => c.WasCalled)
                .Run();

            Assert.AreEqual(TimeSpan.Parse("00:00:10"), context.TTBRUsed);
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public TimeSpan TTBRUsed { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(
                    b =>
                    {
                        b.GetSettings().Set("DisableOutboxTransportCheck", true);
                        b.EnableOutbox();
                    });
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MyMessage message)
                {
                    Context.WasCalled = true;
                }
            }

            public class StartMessageHandler : IHandleMessages<StartMessage>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(StartMessage message)
                {
                    Bus.SendLocal(new MyMessage())
;
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

            public class EndpointConfiguration : IWantToRunBeforeConfigurationIsFinalized
            {
                public static IBuilder builder;

                public void Run(Configure config)
                {
                    builder = config.Builder;
                }
            }


            class SenderWrapper : ISendMessages
            {
                public SenderWrapper(ISendMessages wrappedSender, Context context)
                {
                    this.wrappedSender = wrappedSender;
                    this.context = context;
                }

                public void Send(TransportMessage message, SendOptions sendOptions)
                {
                    if (message.Headers[Headers.EnclosedMessageTypes].Contains("MyMessage"))
                    {
                        context.TTBRUsed = message.TimeToBeReceived;
                    }

                    wrappedSender.Send(message, sendOptions);
                }

                Context context;
                ISendMessages wrappedSender;
            }
        }

        [TimeToBeReceived("00:00:10")]
        public class MyMessage : IMessage
        {
        }

        public class StartMessage : IMessage
        {
        }
    }
}