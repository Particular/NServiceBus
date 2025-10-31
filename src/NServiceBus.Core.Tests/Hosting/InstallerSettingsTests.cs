namespace NServiceBus.Core.Tests.Host;

using System;
using System.Threading;
using System.Threading.Tasks;
using Installation;
using NUnit.Framework;

[TestFixture]
public class InstallerSettingsTests
{
    [Test]
    public void Should_deduplicate_installers_based_on_type()
    {
        var endpointConfiguration = new EndpointConfiguration("myendpoint");

        endpointConfiguration.AddInstaller<MyInstaller>();
        endpointConfiguration.AddInstaller<MyInstaller>();
        endpointConfiguration.AddInstaller<MyInstaller2>();

        var settings = endpointConfiguration.Settings.Get<InstallerComponent.Settings>();

        settings.AddScannedInstallers([typeof(MyInstaller), typeof(MyInstaller2)]);

        Assert.That(settings.Installers.Count, Is.EqualTo(2));
    }

    class MyInstaller : INeedToInstallSomething
    {
        public Task Install(string identity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    class MyInstaller2 : INeedToInstallSomething
    {
        public Task Install(string identity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}