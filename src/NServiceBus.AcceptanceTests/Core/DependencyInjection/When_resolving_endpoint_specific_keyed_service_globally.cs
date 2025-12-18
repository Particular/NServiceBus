namespace NServiceBus.AcceptanceTests.Core.DependencyInjection;

using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Support;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

public class When_resolving_endpoint_specific_keyed_service_globally : NServiceBusAcceptanceTest
{
    // This test verifies that it is possible from the global service provider to resolve endpoint specific keyed services
    // only by providing KeyedServiceKey. This is a very advanced scenario and probably never needed, but the test
    // is here to demonstrate how it would work. But at this stage there is no way to avoid having to provide the KeyedServiceKey.
    [Test]
    public async Task Should_be_possible_using_keyed_service_key()
    {
        var result = await Scenario.Define<Context>()
            .WithEndpoint<ComponentRegistrationEndpoint>(b =>
                b.Services(static services => services.AddKeyedSingleton<IMyComponent, EndpointComponent>(42)))
            .WithServiceResolve(static (provider, _) =>
            {
                var endpointName = AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(ComponentRegistrationEndpoint));
                var context = provider.GetRequiredService<Context>();
                context.KeyedComponent = provider.GetRequiredKeyedService<IMyComponent>(new KeyedServiceKey($"{endpointName}0", 42));
                context.MarkAsCompleted();
                return Task.CompletedTask;
            })
            .Run();

        Assert.That(result.KeyedComponent, Is.InstanceOf<EndpointComponent>());
    }

    class Context : ScenarioContext
    {
        public IMyComponent KeyedComponent { get; set; }
    }

    interface IMyComponent;

    class EndpointComponent : IMyComponent;

    class ComponentRegistrationEndpoint : EndpointConfigurationBuilder
    {
        public ComponentRegistrationEndpoint() => EndpointSetup<DefaultServer>();
    }
}