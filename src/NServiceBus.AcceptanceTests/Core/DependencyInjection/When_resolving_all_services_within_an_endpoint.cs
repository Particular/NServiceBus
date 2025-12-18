namespace NServiceBus.AcceptanceTests.Core.DependencyInjection;

using System.Collections.Generic;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Support;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

public class When_resolving_all_services_within_an_endpoint : NServiceBusAcceptanceTest
{
    // By default, only endpoint-specific registrations are resolved when trying to resolve all services of a given type within an endpoint.
    // However, it should also be possible to resolve globally shared services along with endpoint-specific ones for more advanced scenarios.
    // For example, this allows bypassing the default safeguards put in place to isolate endpoints when there is a valid use case for resolving
    // all services including the globally shared ones.
    [Test]
    public async Task Should_be_possible()
    {
        var result = await Scenario.Define<Context>()
            .WithServices(static services => services.AddSingleton<IMyComponent, SharedComponent>())
            .WithEndpoint<ComponentRegistrationEndpoint>(b =>
                b.Services(static services =>
                    {
                        services.AddSingleton<IMyComponent, EndpointComponent1>();
                        services.AddSingleton<IMyComponent, EndpointComponent2>();
                    })
                    .When((session, _) => session.SendLocal(new SomeMessage())))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Components, Has.Count.EqualTo(3), "All global and endpoint specific services should have been resolved");
            Assert.That(result.Components, Has.One.InstanceOf<EndpointComponent1>().And.One.InstanceOf<EndpointComponent2>().And.One.InstanceOf<SharedComponent>());
        }
    }

    class Context : ScenarioContext
    {
        public IReadOnlyCollection<IMyComponent> Components { get; set; } = [];

        public void MaybeCompleted() => MarkAsCompleted(Components.Count >= 3);
    }

    interface IMyComponent;

    class SharedComponent : IMyComponent;

    class EndpointComponent1 : IMyComponent;
    class EndpointComponent2 : IMyComponent;

    class ComponentRegistrationEndpoint : EndpointConfigurationBuilder
    {
        public ComponentRegistrationEndpoint() => EndpointSetup<DefaultServer>();

        class SomeMessageHandler(Context testContext, [FromKeyedServices(KeyedServiceKey.Any)] IEnumerable<IMyComponent> components) : IHandleMessages<SomeMessage>
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