namespace NServiceBus.AcceptanceTests.DelayedDelivery
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Transports;
    using NUnit.Framework;

    public class When_dispatching_deferred_message_fails_without_dtc : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_retry_delivery()
        {
            var delay = TimeSpan.FromSeconds(5);

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.Given((bus, c) =>
                {
                    var options = new SendOptions();

                    options.DelayDeliveryWith(delay);
                    options.RouteToLocalEndpointInstance();

                    bus.Send(new MyMessage(), options);
                    return Task.FromResult(0);
                }))
                .AllowExceptions(e => e.Message == "simulated exception")
                .Done(c => c.MessageReceived)
                .Run(TimeSpan.FromSeconds(10));

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

                public Task Handle(MyMessage message)
                {
                    context.MessageReceived = true;
                    return Task.FromResult(0);
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
                    var originalDispatcher = EndpointConfiguration.builder.Build<IDispatchMessages>();
                    context.Container.ConfigureComponent(() => new WrappingDispatcher(originalDispatcher), DependencyLifecycle.SingleInstance);
                }
            }

            class WrappingDispatcher : IDispatchMessages
            {
                IDispatchMessages wrappedDispatcher;
                bool failMessage = true;

                public WrappingDispatcher(IDispatchMessages wrappedDispatcher)
                {
                    this.wrappedDispatcher = wrappedDispatcher;
                }

                public void Dispatch(OutgoingMessage message, DispatchOptions dispatchOptions)
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

                    wrappedDispatcher.Dispatch(message, dispatchOptions);
                }
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}
