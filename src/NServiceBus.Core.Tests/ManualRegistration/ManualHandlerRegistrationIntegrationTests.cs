namespace NServiceBus.Core.Tests.ManualRegistration;

using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public class ManualHandlerRegistrationIntegrationTests
{
    [Test]
    public void When_registration_mode_manual_and_handler_manually_registered_should_store_both_settings()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        // Set manual registration mode
        config.UseRegistrationMode(RegistrationMode.Manual);

        // Manually register handler
        config.RegisterHandler<TestMessageHandler>();

        // Verify registration mode is set
        Assert.That(config.GetRegistrationMode(), Is.EqualTo(RegistrationMode.Manual));

        // Verify assembly scanning is disabled (UserProvidedTypes is set to empty array)
        var assemblyScanningConfig = config.Settings.Get<AssemblyScanningComponent.Configuration>();
        Assert.That(assemblyScanningConfig.UserProvidedTypes, Is.Not.Null, "UserProvidedTypes should be set when using manual mode");
        Assert.That(assemblyScanningConfig.UserProvidedTypes, Is.Empty, "UserProvidedTypes should be empty when using manual mode");

        // Verify handler is manually registered
        Assert.That(config.Settings.TryGet(out ManuallyRegisteredHandlers handlers), Is.True, "ManuallyRegisteredHandlers should be created");
        Assert.That(handlers.HandlerTypes, Does.Contain(typeof(TestMessageHandler)), "Handler type should be in the manually registered handlers collection");
    }

    [Test]
    public void When_multiple_handlers_registered_all_should_be_stored()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        config.UseRegistrationMode(RegistrationMode.Manual);
        config.RegisterHandler<TestMessageHandler>();
        config.RegisterHandler<AnotherMessageHandler>();

        Assert.That(config.Settings.TryGet(out ManuallyRegisteredHandlers handlers), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(handlers.HandlerTypes, Has.Count.EqualTo(2));
            Assert.That(handlers.HandlerTypes, Does.Contain(typeof(TestMessageHandler)));
            Assert.That(handlers.HandlerTypes, Does.Contain(typeof(AnotherMessageHandler)));
        });
    }

    [Test]
    public void When_assembly_scanning_enabled_manual_registration_should_still_work()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        // Don't disable assembly scanning - it's on by default
        config.RegisterHandler<TestMessageHandler>();

        // Assembly scanning should still be enabled
        var assemblyScanningConfig = config.Settings.Get<AssemblyScanningComponent.Configuration>();
        Assert.That(assemblyScanningConfig.UserProvidedTypes, Is.Null, "UserProvidedTypes should be null when assembly scanning is enabled");

        // But manual registration should still work
        Assert.That(config.Settings.TryGet(out ManuallyRegisteredHandlers handlers), Is.True);
        Assert.That(handlers.HandlerTypes, Does.Contain(typeof(TestMessageHandler)));
    }

    class TestMessage : IMessage
    {
    }

    class AnotherMessage : IMessage
    {
    }

    class TestMessageHandler : IHandleMessages<TestMessage>
    {
        public Task Handle(TestMessage message, IMessageHandlerContext context)
        {
            return Task.CompletedTask;
        }
    }

    class AnotherMessageHandler : IHandleMessages<AnotherMessage>
    {
        public Task Handle(AnotherMessage message, IMessageHandlerContext context)
        {
            return Task.CompletedTask;
        }
    }
}


