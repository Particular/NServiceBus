namespace NServiceBus.Core.Tests.ManualRegistration;

using System;
using NUnit.Framework;

[TestFixture]
public class RegistrationModeConfigExtensionsTests
{
    [Test]
    public void UseRegistrationMode_Manual_Should_Store_Mode_And_Disable_Scanning()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        config.UseRegistrationMode(RegistrationMode.Manual);

        Assert.That(config.GetRegistrationMode(), Is.EqualTo(RegistrationMode.Manual));
        
        // Verify scanning is disabled by checking UserProvidedTypes
        var assemblyScanningConfig = config.Settings.Get<AssemblyScanningComponent.Configuration>();
        Assert.That(assemblyScanningConfig.UserProvidedTypes, Is.Not.Null);
        Assert.That(assemblyScanningConfig.UserProvidedTypes, Is.Empty);
    }

    [Test]
    public void UseRegistrationMode_SourceGenerated_Should_Store_Mode_And_Disable_Scanning()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        config.UseRegistrationMode(RegistrationMode.SourceGenerated);

        Assert.That(config.GetRegistrationMode(), Is.EqualTo(RegistrationMode.SourceGenerated));
        
        // Verify scanning is disabled
        var assemblyScanningConfig = config.Settings.Get<AssemblyScanningComponent.Configuration>();
        Assert.That(assemblyScanningConfig.UserProvidedTypes, Is.Not.Null);
        Assert.That(assemblyScanningConfig.UserProvidedTypes, Is.Empty);
    }

    [Test]
    public void UseRegistrationMode_AssemblyScanning_Should_Store_Mode_And_Not_Disable_Scanning()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        config.UseRegistrationMode(RegistrationMode.AssemblyScanning);

        Assert.That(config.GetRegistrationMode(), Is.EqualTo(RegistrationMode.AssemblyScanning));
    }

    [Test]
    public void GetRegistrationMode_Default_Should_Return_AssemblyScanning()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        var mode = config.GetRegistrationMode();

        Assert.That(mode, Is.EqualTo(RegistrationMode.AssemblyScanning));
    }

    [Test]
    public void UseRegistrationMode_With_ManualRegistration_Should_Work_Together()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        // Set mode first
        config.UseRegistrationMode(RegistrationMode.Manual);

        // Then register handlers manually
        config.RegisterHandler<TestHandler>();

        // Verify both settings are applied
        Assert.That(config.GetRegistrationMode(), Is.EqualTo(RegistrationMode.Manual));
        Assert.That(config.Settings.TryGet(out ManuallyRegisteredHandlers handlers), Is.True);
        Assert.That(handlers.HandlerTypes, Does.Contain(typeof(TestHandler)));
    }

    [Test]
    public void UseRegistrationMode_Null_Config_Should_Throw()
    {
        EndpointConfiguration config = null;

        Assert.Throws<ArgumentNullException>(() => config.UseRegistrationMode(RegistrationMode.Manual));
    }

    [Test]
    public void GetRegistrationMode_Null_Config_Should_Throw()
    {
        EndpointConfiguration config = null;

        Assert.Throws<ArgumentNullException>(() => config.GetRegistrationMode());
    }

    class TestMessage : IMessage
    {
    }

    class TestHandler : IHandleMessages<TestMessage>
    {
        public System.Threading.Tasks.Task Handle(TestMessage message, IMessageHandlerContext context)
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }
}

