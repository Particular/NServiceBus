namespace NServiceBus.AcceptanceTests.Core.DependencyInjection;

using System.Collections.Generic;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

public class When_resolving_services_globally : NServiceBusAcceptanceTest
{
    // On the root level you can get access to all registrations, including the globally shared ones.
    // Demonstrating here with a custom component, but technically it is possible to resolve any service globally,
    // including the public ones the NServiceBus core registers.
    [Test]
    public async Task Should_be_possible_to_get_all()
    {
        var result = await Scenario.Define<Context>()
            .WithServices(static services => services.AddSingleton<IMyComponent, SharedComponent>())
            .WithEndpoint<ComponentRegistrationEndpoint>(b =>
                b.Services(static services =>
                    {
                        services.AddSingleton<IMyComponent, EndpointComponent1>();
                        services.AddSingleton<IMyComponent, EndpointComponent2>();
                    }))
            .WithServiceResolve(static (provider, _) =>
            {
                var context = provider.GetRequiredService<Context>();
                var allServices = new List<IMyComponent>();
                // demonstrates using GetServices to get the non-keyed services of that type
                allServices.AddRange(provider.GetServices<IMyComponent>());
                // demonstrates using AnyKey to get the keyed services of that type
                allServices.AddRange(provider.GetKeyedServices<IMyComponent>(KeyedService.AnyKey));
                context.Components = allServices;
                context.MaybeCompleted();
                return Task.CompletedTask;
            })
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Components, Has.Count.EqualTo(3), "All global and endpoint specific services should have been resolved");
            Assert.That(result.Components, Has.One.InstanceOf<EndpointComponent1>().And.One.InstanceOf<EndpointComponent2>().And.One.InstanceOf<SharedComponent>());
        }
    }

    class Context : ScenarioContext
    {
        public List<IMyComponent> Components { get; set; } = [];
        public void MaybeCompleted() => MarkAsCompleted(Components.Count >= 3);
    }

    interface IMyComponent;

    class SharedComponent : IMyComponent;

    class EndpointComponent1 : IMyComponent;
    class EndpointComponent2 : IMyComponent;

    class ComponentRegistrationEndpoint : EndpointConfigurationBuilder
    {
        public ComponentRegistrationEndpoint() => EndpointSetup<DefaultServer>();
    }
}