#pragma warning disable CS0618 // Type or member is obsolete
namespace NServiceBus.AcceptanceTests.Core.Installers;

using System.Threading.Tasks;
using AcceptanceTesting.Customization;
using FakeTransport;
using Installation;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

public class When_using_obsolete_installer_setup : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_run_installers_and_set_up_infrastructure_without_starting_receivers()
    {
        bool? setupInfrastructure = null;
        var fakeTransport = new FakeTransport { OnTransportInitialize = t => setupInfrastructure = t.setupInfrastructure };
        var endpointConfiguration = InstallerTestHelpers.CreateStartableEndpointConfiguration(fakeTransport, "EndpointWithObsoleteInstallerSetup");
        var context = new RecordingInstallerContext();
        endpointConfiguration.RegisterScenarioContext(context);

        await Installer.Setup(endpointConfiguration);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.InstallerCalled, Is.True, "Should run installers");
            Assert.That(setupInfrastructure, Is.True, "Should set up transport infrastructure");
        }

        fakeTransport.AssertDidNotStartReceivers();
    }

    [Test]
    public async Task Should_run_installers_and_set_up_infrastructure_when_using_externally_managed_container()
    {
        bool? setupInfrastructure = null;
        var fakeTransport = new FakeTransport { OnTransportInitialize = t => setupInfrastructure = t.setupInfrastructure };
        var endpointConfiguration = InstallerTestHelpers.CreateStartableEndpointConfiguration(fakeTransport, "EndpointWithObsoleteExternalInstallerSetup");
        var context = new RecordingInstallerContext();
        endpointConfiguration.RegisterScenarioContext(context);
        var services = new ServiceCollection();

        var installer = Installer.CreateInstallerWithExternallyManagedContainer(endpointConfiguration, services);
        await using var provider = services.BuildServiceProvider();
        await installer.Setup(provider);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.InstallerCalled, Is.True, "Should run installers");
            Assert.That(setupInfrastructure, Is.True, "Should set up transport infrastructure");
        }

        fakeTransport.AssertDidNotStartReceivers();
    }
}