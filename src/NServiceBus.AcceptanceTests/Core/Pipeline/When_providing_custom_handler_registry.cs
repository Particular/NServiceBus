namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvancedExtensibility;
    using EndpointTemplates;
    using NUnit.Framework;
    using Unicast;

    public class When_providing_custom_handler_registry : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_invoke_manually_registered_handlers()
        {
            Requires.NativePubSubSupport();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithRegularHandler>(e => e
                    .When(ctx => ctx.SendLocal(new SomeCommand()))
                    .When(ctx => ctx.Publish(new SomeEvent()))) // verify autosubscribe picks up the handlers too
                .Done(c => c.RegularCommandHandlerInvoked
                           && c.ManuallyRegisteredCommandHandlerInvoked
                           && c.RegularEventHandlerInvoked
                           && c.ManuallyRegisteredEventHandlerInvoked)
                .Run(TimeSpan.FromSeconds(10));

            Assert.IsTrue(context.RegularCommandHandlerInvoked);
            Assert.IsTrue(context.ManuallyRegisteredCommandHandlerInvoked);
            Assert.IsTrue(context.RegularEventHandlerInvoked);
            Assert.IsTrue(context.ManuallyRegisteredEventHandlerInvoked);
        }

        class Context : ScenarioContext
        {
            public bool RegularCommandHandlerInvoked { get; set; }
            public bool ManuallyRegisteredCommandHandlerInvoked { get; set; }
            public bool RegularEventHandlerInvoked { get; set; }
            public bool ManuallyRegisteredEventHandlerInvoked { get; set; }
        }

        class EndpointWithRegularHandler : EndpointConfigurationBuilder
        {
            public EndpointWithRegularHandler()
            {
                EndpointSetup<DefaultServer>(c =>
                    {
                        var registry = new MessageHandlerRegistry();
                        registry.RegisterHandler(typeof(ManuallyRegisteredHandler));
                        c.GetSettings().Set(registry);
                        // the handler isn't registered for DI automatically
                        c.RegisterComponents(components => components
                            .ConfigureComponent<ManuallyRegisteredHandler>(DependencyLifecycle.InstancePerCall));
                    })
                    .ExcludeType<ManuallyRegisteredHandler>();
            }

            class RegularHandler : IHandleMessages<SomeCommand>, IHandleMessages<SomeEvent>
            {
                Context testContext;

                public RegularHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(SomeCommand message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.RegularCommandHandlerInvoked = true;
                    return Task.FromResult(0);
                }

                public Task Handle(SomeEvent message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.RegularEventHandlerInvoked = true;
                    return Task.FromResult(0);
                }
            }
        }

        class ManuallyRegisteredHandler : IHandleMessages<SomeCommand>, IHandleMessages<SomeEvent>
        {
            Context testContext;

            public ManuallyRegisteredHandler(Context testContext)
            {
                this.testContext = testContext;
            }

            public Task Handle(SomeCommand message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
            {
                testContext.ManuallyRegisteredCommandHandlerInvoked = true;
                return Task.FromResult(0);
            }

            public Task Handle(SomeEvent message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
            {
                testContext.ManuallyRegisteredEventHandlerInvoked = true;
                return Task.FromResult(0);
            }
        }

        public class SomeCommand : ICommand
        {
        }

        public class SomeEvent : IEvent
        {
        }
    }
}