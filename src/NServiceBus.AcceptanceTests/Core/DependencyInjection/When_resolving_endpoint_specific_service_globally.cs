namespace NServiceBus.AcceptanceTests.Core.DependencyInjection;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

public class When_resolving_endpoint_specific_service_globally : NServiceBusAcceptanceTest
{
    // This test verifies that it is possible from the global service provider to resolve keys without having to
    // pass them as KeyedServiceKeys.
    [Test]
    public async Task Should_be_possible()
    {
        var result = await Scenario.Define<Context>()
            .WithEndpoint<ComponentRegistrationEndpoint>(b =>
                b.Services(static services => services.AddSingleton<IMyComponent, EndpointComponent>()))
            .WithServiceResolve(static (provider, _) =>
            {
                var endpointName = AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(ComponentRegistrationEndpoint));
                var context = provider.GetRequiredService<Context>();
                context.Component = provider.GetRequiredKeyedService<IMyComponent>($"{endpointName}0");
                context.MarkAsCompleted();
                return Task.CompletedTask;
            })
            .Run();

        Assert.That(result.Component, Is.InstanceOf<EndpointComponent>());
    }

    class Context : ScenarioContext
    {
        public IMyComponent Component { get; set; }
    }

    interface IMyComponent;

    class EndpointComponent : IMyComponent;

    class ComponentRegistrationEndpoint : EndpointConfigurationBuilder
    {
        public ComponentRegistrationEndpoint() => EndpointSetup<DefaultServer>();
    }
}