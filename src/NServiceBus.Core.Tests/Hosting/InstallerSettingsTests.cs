namespace NServiceBus.Core.Tests.Host;

using System;
using System.Threading;
using System.Threading.Tasks;
using Installation;
using NServiceBus.Support;
using NUnit.Framework;
using NUnit.Framework.Legacy;

[TestFixture]
public class InstallerSettingsTests
{
    [Test]
    public void Should_deduplicate_installers()
    {
        var endpointConfiguration = new EndpointConfiguration("myendpoint");

        endpointConfiguration.RegisterInstaller<MyInstaller>();
        endpointConfiguration.RegisterInstaller<MyInstaller>();

        var installers = endpointConfiguration.Settings.Get<InstallerComponent.Settings>().Installers;

        Assert.That(installers.Count, Is.EqualTo(1));
    }

    class MyInstaller : INeedToInstallSomething
    {
        public Task Install(string identity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}