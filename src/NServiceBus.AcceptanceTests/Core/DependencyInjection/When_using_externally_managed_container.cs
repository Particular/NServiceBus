namespace NServiceBus.AcceptanceTests.Core.DependencyInjection;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

public class When_using_externally_managed_container : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_use_it_for_component_resolution()
    {
        var serviceCollection = new ServiceCollection();
        var myComponent = new MyComponent();

        serviceCollection.AddSingleton(myComponent);

        await Scenario.Define<ScenarioContext>()
            .WithEndpoint<ExternallyManagedContainerEndpoint>(b =>
            {
                b.ToCreateInstance(
                        config => EndpointWithExternallyManagedContainer.Create(config, serviceCollection),
                        (configured, ct) => configured.Start(serviceCollection.BuildServiceProvider(), ct)
                    )
                    .When((serviceProvider, _, _) =>
                    {
                        Assert.That(serviceProvider.GetRequiredService<MyComponent>(), Is.SameAs(myComponent), "Should be able to resolve components from the external container");
                        return Task.CompletedTask;
                    });
            })
            .Done(c => c.EndpointsStarted)
            .Run();
    }

    public class ExternallyManagedContainerEndpoint : EndpointConfigurationBuilder
    {
        public ExternallyManagedContainerEndpoint() => EndpointSetup<DefaultServer>();
    }

    public class MyComponent
    {
    }
}