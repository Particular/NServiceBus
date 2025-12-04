namespace NServiceBus.AcceptanceTests.Core.DependencyInjection;

using System.Collections.Generic;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Support;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

public class When_resolving_services_within_an_endpoint : NServiceBusAcceptanceTest
{
    // By default, only endpoint-specific registrations should be resolved which is slightly counterintuitive knowing how
    // the default GetServices works in Microsoft.Extensions.DependencyInjection but crucial for proper isolation between endpoints.
    // For example, consider Core doing GetServices<IMutator> would return otherwise all mutators from all endpoints which is not desired.
    [Test]
    public async Task Should_get_endpoint_specific_dependencies_only()
    {
        var result = await Scenario.Define<Context>()
            .WithComponent(new ComponentThatRegistersGloballySharedServices())
            .WithEndpoint<ComponentRegistrationEndpoint>(b =>
                b.Services(static services =>
                    {
                        services.AddSingleton<IMyComponent, EndpointComponent1>();
                        services.AddSingleton<IMyComponent, EndpointComponent2>();
                    })
                    .When((session, c) => session.SendLocal(new SomeMessage())))
            .Done(c => c.Components.Count != 0)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Components, Has.Count.EqualTo(2), "Endpoint specific services should have been resolved");
            Assert.That(result.Components, Has.One.InstanceOf<EndpointComponent1>().And.One.InstanceOf<EndpointComponent2>().And.No.InstanceOf<SharedComponent>());
        }
    }

    class Context : ScenarioContext
    {
        public IReadOnlyCollection<IMyComponent> Components { get; set; } = [];
    }

    interface IMyComponent;

    class SharedComponent : IMyComponent;

    class EndpointComponent1 : IMyComponent;
    class EndpointComponent2 : IMyComponent;

    class ComponentThatRegistersGloballySharedServices : ComponentRunner, IComponentBehavior
    {
        public Task<ComponentRunner> CreateRunner(RunDescriptor run)
        {
            run.Services.AddSingleton<IMyComponent, SharedComponent>();
            return Task.FromResult<ComponentRunner>(this);
        }

        public override string Name => nameof(ComponentThatRegistersGloballySharedServices);
    }

    class ComponentRegistrationEndpoint : EndpointConfigurationBuilder
    {
        public ComponentRegistrationEndpoint() => EndpointSetup<DefaultServer>();

        class SomeMessageHandler(Context testContext, IEnumerable<IMyComponent> components) : IHandleMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                testContext.Components = [.. components];
                return Task.CompletedTask;
            }
        }
    }

    public class SomeMessage : ICommand;
}