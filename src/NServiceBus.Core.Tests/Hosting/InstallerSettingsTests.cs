namespace NServiceBus.Core.Tests.Host;

using System;
using System.Threading;
using System.Threading.Tasks;
using Configuration.AdvancedExtensibility;
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

        Assert.That(settings.Installers, Has.Count.EqualTo(2));
    }

    [Test]
    public void Should_default_to_username()
    {
        var endpointConfiguration = new EndpointConfiguration("myendpoint");

        endpointConfiguration.EnableInstallers();

        Assert.That(endpointConfiguration.Settings.Get<InstallerComponent.Settings>().InstallationUserName, Is.EqualTo(InstallerComponent.Settings.DefaultUsername));
    }

    [Test]
    public void Should_expose_username_over_settings()
    {
        var endpointConfiguration = new EndpointConfiguration("myendpoint");

        var username = "MyUsername";
        endpointConfiguration.EnableInstallers(username);

        Assert.That(endpointConfiguration.GetSettings().Get(InstallerComponent.Settings.UsernameSettingsKey), Is.EqualTo(username));
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