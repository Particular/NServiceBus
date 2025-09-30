namespace NServiceBus.Core.Tests.ManualRegistration;

using System;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public class ManualRegistrationConfigExtensionsTests
{
    [Test]
    public void RegisterHandler_Generic_Should_Store_Handler_Type()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        config.RegisterHandler<MyMessageHandler>();

        Assert.That(config.Settings.TryGet(out ManuallyRegisteredHandlers handlers), Is.True);
        Assert.That(handlers.HandlerTypes, Does.Contain(typeof(MyMessageHandler)));
    }

    [Test]
    public void RegisterHandler_NonGeneric_Should_Store_Handler_Type()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        config.RegisterHandler(typeof(MyMessageHandler));

        Assert.That(config.Settings.TryGet(out ManuallyRegisteredHandlers handlers), Is.True);
        Assert.That(handlers.HandlerTypes, Does.Contain(typeof(MyMessageHandler)));
    }

    [Test]
    public void RegisterHandler_Multiple_Should_Store_All_Handler_Types()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        config.RegisterHandler<MyMessageHandler>();
        config.RegisterHandler<MyOtherMessageHandler>();

        Assert.That(config.Settings.TryGet(out ManuallyRegisteredHandlers handlers), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(handlers.HandlerTypes, Does.Contain(typeof(MyMessageHandler)));
            Assert.That(handlers.HandlerTypes, Does.Contain(typeof(MyOtherMessageHandler)));
            Assert.That(handlers.HandlerTypes.Count, Is.EqualTo(2));
        });
    }

    [Test]
    public void RegisterHandler_Null_Should_Throw()
    {
        var config = new EndpointConfiguration("TestEndpoint");

        Assert.Throws<ArgumentNullException>(() => config.RegisterHandler(null));
    }

    class MyMessage : IMessage
    {
    }

    class MyOtherMessage : IMessage
    {
    }

    class MyMessageHandler : IHandleMessages<MyMessage>
    {
        public Task Handle(MyMessage message, IMessageHandlerContext context)
        {
            return Task.CompletedTask;
        }
    }

    class MyOtherMessageHandler : IHandleMessages<MyOtherMessage>
    {
        public Task Handle(MyOtherMessage message, IMessageHandlerContext context)
        {
            return Task.CompletedTask;
        }
    }
}
