#pragma warning disable CS0618 // Type or member is obsolete
namespace NServiceBus.AcceptanceTests.Core.TransportSeam;

using System.Threading;
using System.Threading.Tasks;
using FakeTransport;
using Installation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using static LifecycleRecordingEndpoint;

public class When_running_installers_only : NServiceBusAcceptanceTest
{
    [Test]
    [CancelAfter(30_000)]
    public async Task Should_shut_down_the_transport_when_hosted(CancellationToken cancellationToken = default)
    {
        var fakeTransport = new FakeTransport();
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddNServiceBusEndpoint(Create(fakeTransport, "InstallersOnlyHosted"));
        builder.Services.AddNServiceBusInstallers();

        using var host = builder.Build();
        await host.RunAsync(cancellationToken);

        AssertInitializedAndShutDown(fakeTransport);
    }

    [Test]
    public async Task Should_shut_down_the_transport_when_using_obsolete_installer_setup()
    {
        var fakeTransport = new FakeTransport();

        await Installer.Setup(Create(fakeTransport, "InstallersOnlyObsolete"));

        AssertInitializedAndShutDown(fakeTransport);
    }

    [Test]
    public async Task Should_shut_down_the_transport_when_using_obsolete_installer_with_externally_managed_container()
    {
        var fakeTransport = new FakeTransport();
        var services = new ServiceCollection();

        var installer = Installer.CreateInstallerWithExternallyManagedContainer(Create(fakeTransport, "InstallersOnlyObsoleteExternal"), services);
        await using (var provider = services.BuildServiceProvider())
        {
            await installer.Setup(provider);
        }

        AssertInitializedAndShutDown(fakeTransport);
    }

    static void AssertInitializedAndShutDown(FakeTransport fakeTransport)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(fakeTransport.StartupSequence, Does.Contain(TransportInitialize));
            Assert.That(fakeTransport.StartupSequence, Does.Not.Contain(StartMain), "Installers-only runs should not start receivers");
            Assert.That(fakeTransport.StartupSequence, Does.Contain(TransportShutdown), "Sequence: " + string.Join(" -> ", fakeTransport.StartupSequence));
        }
    }
}