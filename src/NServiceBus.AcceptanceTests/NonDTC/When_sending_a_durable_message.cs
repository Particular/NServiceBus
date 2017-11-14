namespace NServiceBus.AcceptanceTests.NonDTC
{
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Features;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;
    using NUnit.Framework;

    public class When_sending_a_durable_message : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_honor_it()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<Endpoint>(b => b.Given((bus, c) => bus.SendLocal(new StartMessage())))
                .Done(c => c.WasCalled)
                .Run();

            Assert.True(context.WasSentDurable);
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public bool WasSentDurable { get; set; }
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

            public class MyMessageHandler : IHandleMessages<MyDurableMessage>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MyDurableMessage message)
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
                    Bus.SendLocal(new MyDurableMessage());
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
                    context.WasSentDurable = message.Recoverable;

                    wrappedSender.Send(message, sendOptions);
                }

                Context context;
                ISendMessages wrappedSender;
            }
        }

        public class MyDurableMessage : IMessage
        {
        }

        public class StartMessage : IMessage
        {
        }
    }
}