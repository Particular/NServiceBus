namespace NServiceBus.AcceptanceTests.Core.Pipeline;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using Configuration.AdvancedExtensibility;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Unicast;

public class When_providing_custom_handler_registry : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_invoke_manually_registered_handlers()
    {
        Requires.MessageDrivenPubSub();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithRegularHandler>(e => e
                .When(s => s.SendLocal(new SomeCommand()))
                .When(ctx => ctx.EventSubscribed, s => s.Publish(new SomeEvent()))) // verify autosubscribe picks up the handlers too
            .Done(c => c.RegularCommandHandlerInvoked
                       && c.ManuallyRegisteredCommandHandlerInvoked
                       && c.RegularEventHandlerInvoked
                       && c.ManuallyRegisteredEventHandlerInvoked)
            .Run(TimeSpan.FromSeconds(10));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.RegularCommandHandlerInvoked, Is.True);
            Assert.That(context.ManuallyRegisteredCommandHandlerInvoked, Is.True);
            Assert.That(context.RegularEventHandlerInvoked, Is.True);
            Assert.That(context.ManuallyRegisteredEventHandlerInvoked, Is.True);
        }
    }

    class Context : ScenarioContext
    {
        public bool RegularCommandHandlerInvoked { get; set; }
        public bool ManuallyRegisteredCommandHandlerInvoked { get; set; }
        public bool RegularEventHandlerInvoked { get; set; }
        public bool ManuallyRegisteredEventHandlerInvoked { get; set; }
        public bool EventSubscribed { get; set; }
    }

    class EndpointWithRegularHandler : EndpointConfigurationBuilder
    {
        public EndpointWithRegularHandler()
        {
            EndpointSetup<DefaultServer>(c =>
                {
                    var registry = new MessageHandlerRegistry();
                    registry.AddHandler<ManuallyRegisteredHandler>();
                    c.GetSettings().Set(registry);
                    // the handler isn't registered for DI automatically
                    c.RegisterComponents(components => components
                        .AddTransient<ManuallyRegisteredHandler>());
                    c.OnEndpointSubscribed<Context>((t, ctx) =>
                    {
                        if (t.MessageType == typeof(SomeEvent).AssemblyQualifiedName)
                        {
                            ctx.EventSubscribed = true;
                        }
                    });
                }, metadata => metadata.RegisterPublisherFor<SomeEvent>(AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(EndpointWithRegularHandler))))
                .ExcludeType<ManuallyRegisteredHandler>();
        }

        class RegularHandler : IHandleMessages<SomeCommand>, IHandleMessages<SomeEvent>
        {
            Context testContext;

            public RegularHandler(Context testContext)
            {
                this.testContext = testContext;
            }

            public Task Handle(SomeCommand message, IMessageHandlerContext context)
            {
                testContext.RegularCommandHandlerInvoked = true;
                return Task.CompletedTask;
            }

            public Task Handle(SomeEvent message, IMessageHandlerContext context)
            {
                testContext.RegularEventHandlerInvoked = true;
                return Task.CompletedTask;
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

        public Task Handle(SomeCommand message, IMessageHandlerContext context)
        {
            testContext.ManuallyRegisteredCommandHandlerInvoked = true;
            return Task.CompletedTask;
        }

        public Task Handle(SomeEvent message, IMessageHandlerContext context)
        {
            testContext.ManuallyRegisteredEventHandlerInvoked = true;
            return Task.CompletedTask;
        }
    }

    public class SomeCommand : ICommand
    {
    }

    public class SomeEvent : IEvent
    {
    }
}