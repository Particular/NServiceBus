namespace NServiceBus.AcceptanceTests.Core.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Support;
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
            .WithComponent(new ComponentThatRegistersGloballySharedServices())
            .WithEndpoint<ComponentRegistrationEndpoint>(b =>
                b.Services(static services =>
                    {
                        services.AddSingleton<IMyComponent, EndpointComponent1>();
                        services.AddSingleton<IMyComponent, EndpointComponent2>();
                    }))
            .Done(c => c.Components.Count != 0)
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
    }

    interface IMyComponent;

    class SharedComponent : IMyComponent;

    class EndpointComponent1 : IMyComponent;
    class EndpointComponent2 : IMyComponent;

    class ComponentThatRegistersGloballySharedServices : ComponentRunner, IComponentBehavior
    {
        RunDescriptor runDescriptor;

        public Task<ComponentRunner> CreateRunner(RunDescriptor run)
        {
            run.Services.AddSingleton<IMyComponent, SharedComponent>();
            runDescriptor = run;
            return Task.FromResult<ComponentRunner>(this);
        }

        public override Task ComponentsStarted(CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(runDescriptor);
            ArgumentNullException.ThrowIfNull(runDescriptor.ServiceProvider);

            var context = runDescriptor.ServiceProvider.GetRequiredService<Context>();
            var allServices = new List<IMyComponent>();
            // demonstrates using GetServices to get the non-keyed services of that type
            allServices.AddRange(runDescriptor.ServiceProvider.GetServices<IMyComponent>());
            // demonstrates using AnyKey to get the keyed services of that type
            allServices.AddRange(runDescriptor.ServiceProvider.GetKeyedServices<IMyComponent>(KeyedService.AnyKey));
            context.Components = allServices;
            return Task.CompletedTask;
        }

        public override string Name => nameof(ComponentThatRegistersGloballySharedServices);
    }

    class ComponentRegistrationEndpoint : EndpointConfigurationBuilder
    {
        public ComponentRegistrationEndpoint() => EndpointSetup<DefaultServer>();
    }
}