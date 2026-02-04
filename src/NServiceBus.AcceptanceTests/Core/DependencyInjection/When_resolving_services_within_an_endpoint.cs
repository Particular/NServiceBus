namespace NServiceBus.AcceptanceTests.Core.DependencyInjection;

using System.Collections.Generic;
using System.Threading.Tasks;
using AcceptanceTesting;
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
            .WithServices(static services => services.AddSingleton<IMyComponent, SharedComponent>())
            .WithEndpoint<ComponentRegistrationEndpoint>(b =>
                b.Services(static services =>
                    {
                        services.AddSingleton<IMyComponent, EndpointComponent1>();
                        services.AddSingleton<IMyComponent, EndpointComponent2>();
                    })
                    .When((session, c) => session.SendLocal(new SomeMessage())))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Components, Has.Count.EqualTo(2), "Endpoint specific services should have been resolved");
            Assert.That(result.Components, Has.One.InstanceOf<EndpointComponent1>().And.One.InstanceOf<EndpointComponent2>().And.No.InstanceOf<SharedComponent>());
        }
    }

    public class Context : ScenarioContext
    {
        public IReadOnlyCollection<IMyComponent> Components { get; set; } = [];
        public void MaybeCompleted() => MarkAsCompleted(Components.Count >= 2);
    }

    public interface IMyComponent;

    public class SharedComponent : IMyComponent;

    public class EndpointComponent1 : IMyComponent;
    public class EndpointComponent2 : IMyComponent;

    public class ComponentRegistrationEndpoint : EndpointConfigurationBuilder
    {
        public ComponentRegistrationEndpoint() => EndpointSetup<DefaultServer>();

        [Handler]
        public class SomeMessageHandler(Context testContext, IEnumerable<IMyComponent> components) : IHandleMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                testContext.Components = [.. components];
                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class SomeMessage : ICommand;
}