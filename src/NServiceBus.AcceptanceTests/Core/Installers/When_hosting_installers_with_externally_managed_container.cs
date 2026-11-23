#pragma warning disable CS0618 // Type or member is obsolete
namespace NServiceBus.AcceptanceTests.Core.Installers;

using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using FakeTransport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

public class When_hosting_installers_with_externally_managed_container : NServiceBusAcceptanceTest
{
    // AddNServiceBusInstallers is a host-level setting (#7709), so any endpoint created from the same
    // provider runs installers. Only EndpointHostedService skips Start, and this endpoint isn't hosted by it.
    [Test]
    public async Task Should_run_installers_and_set_up_infrastructure_when_installers_registered_in_the_container()
    {
        bool? setupInfrastructure = null;
        var fakeTransport = new FakeTransport { OnTransportInitialize = t => setupInfrastructure = t.setupInfrastructure };
        var endpointConfiguration = InstallerTestHelpers.CreateStartableEndpointConfiguration(fakeTransport, "ExternallyManagedWithInstallers");
        IStartableEndpointWithExternallyManagedContainer startableEndpoint = null;

        var context = await Scenario.Define<RecordingInstallerContext>(endpointConfiguration.RegisterScenarioContext)
            .WithServices(services =>
            {
                services.AddSingleton<IHostApplicationLifetime, FakeHostApplicationLifetime>();
                services.AddNServiceBusInstallers();
                startableEndpoint = EndpointWithExternallyManagedContainer.Create(endpointConfiguration, services);
            })
            .WithServiceResolve(async (provider, token) =>
            {
                var endpoint = await startableEndpoint.Start(provider, token);
                await endpoint.Stop(token);
            })
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.InstallerCalled, Is.True, "Should run installers");
            Assert.That(setupInfrastructure, Is.True, "Should set up transport infrastructure");
            Assert.That(fakeTransport.StartupSequence, Does.Contain("IMessageReceiver.StartReceive for receiver Main"), "Should start the receivers because the caller started the endpoint");
        }
    }
}