namespace NServiceBus.Core.Tests.ManualRegistration;

using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Installation;
using NUnit.Framework;

[TestFixture]
public class RegisterInstallerTests
{
    [Test]
    public void RegisterInstaller_Generic_Should_Store_Installer_Type()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        config.RegisterInstaller<TestInstaller>();

        Assert.That(config.Settings.TryGet(out ManuallyRegisteredInstallers installers), Is.True);
        Assert.That(installers.InstallerTypes, Has.Count.EqualTo(1));
        Assert.That(installers.InstallerTypes, Does.Contain(typeof(TestInstaller)));
    }

    [Test]
    public void RegisterInstaller_NonGeneric_Should_Store_Installer_Type()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        config.RegisterInstaller(typeof(TestInstaller));

        Assert.That(config.Settings.TryGet(out ManuallyRegisteredInstallers installers), Is.True);
        Assert.That(installers.InstallerTypes, Has.Count.EqualTo(1));
        Assert.That(installers.InstallerTypes, Does.Contain(typeof(TestInstaller)));
    }

    [Test]
    public void RegisterInstaller_Multiple_Should_Store_All()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        config.RegisterInstaller<TestInstaller>();
        config.RegisterInstaller<AnotherTestInstaller>();

        Assert.That(config.Settings.TryGet(out ManuallyRegisteredInstallers installers), Is.True);
        Assert.That(installers.InstallerTypes, Has.Count.EqualTo(2));
        Assert.That(installers.InstallerTypes, Does.Contain(typeof(TestInstaller)));
        Assert.That(installers.InstallerTypes, Does.Contain(typeof(AnotherTestInstaller)));
    }

    [Test]
    public void RegisterInstaller_Null_InstallerType_Should_Throw()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        Assert.Throws<ArgumentNullException>(() => config.RegisterInstaller(null));
    }

    public class TestInstaller : INeedToInstallSomething
    {
        public Task Install(string identity, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    public class AnotherTestInstaller : INeedToInstallSomething
    {
        public Task Install(string identity, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}

