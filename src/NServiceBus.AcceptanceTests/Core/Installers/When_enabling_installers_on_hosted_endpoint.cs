namespace NServiceBus.AcceptanceTests.Core.Installers;

using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using FakeTransport;
using NUnit.Framework;

public class When_enabling_installers_on_hosted_endpoint : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_run_installers_set_up_infrastructure_and_start_receivers()
    {
        bool? setupInfrastructure = null;
        var fakeTransport = new FakeTransport { OnTransportInitialize = t => setupInfrastructure = t.setupInfrastructure };
        var endpointConfiguration = InstallerTestHelpers.CreateStartableEndpointConfiguration(fakeTransport, "HostedEndpointWithInstallersEnabled");
        endpointConfiguration.EnableInstallers();

        var context = await Scenario.Define<RecordingInstallerContext>(endpointConfiguration.RegisterScenarioContext)
            .WithServices(services => services.AddNServiceBusEndpoint(endpointConfiguration))
            .WithServiceResolve(static async (provider, token) => await provider.RunHostedServices(token))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.InstallerCalled, Is.True, "Should run installers");
            Assert.That(setupInfrastructure, Is.True, "Should set up transport infrastructure");
            Assert.That(fakeTransport.StartupSequence, Does.Contain("IMessageReceiver.StartReceive for receiver Main"), "Should start the receivers");
        }
    }
}