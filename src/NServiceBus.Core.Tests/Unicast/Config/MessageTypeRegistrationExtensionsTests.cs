namespace NServiceBus.Unicast.Tests;

using System;
using System.Linq;
using NUnit.Framework;
using Unicast.Messages;

[TestFixture]
public class MessageTypeRegistrationExtensionsTests
{
    [Test]
    public void Should_register_message_type_with_runtime_inferred_hierarchy()
    {
        var config = new EndpointConfiguration("test");
        config.AddMessageType<MyEvent>();

        var registry = config.Settings.GetOrCreate<MessageMetadataRegistry>();
        registry.Initialize(new Conventions().IsMessageType, true);

        var metadata = registry.GetMessageMetadata(typeof(MyEvent));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(metadata.MessageType, Is.EqualTo(typeof(MyEvent)));
            Assert.That(metadata.MessageHierarchy, Is.EqualTo(new[] { typeof(MyEvent), typeof(IInterfaceParent1), typeof(ConcreteParent1), typeof(IInterfaceParent1Base), typeof(ConcreteParentBase) }));
        }
    }

    [Test]
    public void Should_register_message_type_when_registry_is_already_initialized()
    {
        var config = new EndpointConfiguration("test");
        var registry = config.Settings.GetOrCreate<MessageMetadataRegistry>();
        registry.Initialize(new Conventions().IsMessageType, true);

        config.AddMessageType<MyEvent>();

        Assert.That(registry.GetAllMessages().Select(m => m.MessageType), Does.Contain(typeof(MyEvent)));
    }

    [Test]
    public void Should_throw_when_configuration_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => MessageTypeRegistrationExtensions.AddMessageType<MyEvent>(null));
    }

    class MyEvent : ConcreteParent1, IInterfaceParent1;
    class ConcreteParent1 : ConcreteParentBase;
    class ConcreteParentBase : IMessage;
    interface IInterfaceParent1 : IInterfaceParent1Base;
    interface IInterfaceParent1Base : IMessage;
}
