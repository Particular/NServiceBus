namespace NServiceBus.Unicast.Tests;

using System;
using System.Linq;
using NUnit.Framework;
using Unicast.Messages;

[TestFixture]
public class MessageMetadataRegistryCharacterizationTests
{
    [Test]
    public void Should_return_the_same_cached_metadata_instance_for_a_registered_concrete_type()
    {
        var registry = new MessageMetadataRegistry();
        registry.Initialize(new Conventions().IsMessageType, true);
        registry.RegisterMessageTypes([typeof(MyEvent)]);

        var first = registry.GetMessageMetadata(typeof(MyEvent));
        var second = registry.GetMessageMetadata(typeof(MyEvent));

        Assert.That(second, Is.SameAs(first));
    }

    [Test]
    public void Should_include_registered_concrete_types_in_get_all_messages()
    {
        var registry = new MessageMetadataRegistry();
        registry.Initialize(new Conventions().IsMessageType, true);
        registry.RegisterMessageTypes([typeof(MyEvent), typeof(MyOtherEvent)]);

        var allMessageTypes = registry.GetAllMessages().Select(m => m.MessageType).ToList();

        Assert.That(allMessageTypes, Does.Contain(typeof(MyEvent)).And.Contain(typeof(MyOtherEvent)));
    }

    [Test]
    public void Should_use_the_explicitly_supplied_hierarchy_when_registered_before_initialization()
    {
        var registry = new MessageMetadataRegistry();
        registry.RegisterMessageTypeWithHierarchy(typeof(MyEvent), [typeof(ConcreteParent1), typeof(IMessage)]);
        registry.Initialize(new Conventions().IsMessageType, true);

        var messageMetadata = registry.GetMessageMetadata(typeof(MyEvent));

        Assert.That(messageMetadata.MessageHierarchy, Is.EqualTo(new[] { typeof(MyEvent), typeof(ConcreteParent1) }));
    }

    [Test]
    public void Should_infer_the_hierarchy_at_runtime_when_registered_before_initialization_without_hierarchy()
    {
        var registry = new MessageMetadataRegistry();
        registry.RegisterMessageTypes([typeof(MyEvent)]);
        registry.Initialize(new Conventions().IsMessageType, true);

        var messageMetadata = registry.GetMessageMetadata(typeof(MyEvent));

        Assert.That(messageMetadata.MessageHierarchy, Is.EqualTo(new[] { typeof(MyEvent), typeof(IInterfaceParent1), typeof(ConcreteParent1), typeof(IInterfaceParent1Base), typeof(ConcreteParentBase) }));
    }

    [Test]
    public void Should_resolve_and_register_unregistered_concrete_types_on_demand()
    {
        var registry = new MessageMetadataRegistry();
        registry.Initialize(new Conventions().IsMessageType, false);

        var messageMetadata = registry.GetMessageMetadata(typeof(MyEvent));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(messageMetadata.MessageType, Is.EqualTo(typeof(MyEvent)));
            Assert.That(messageMetadata.MessageHierarchy, Is.EqualTo(new[] { typeof(MyEvent), typeof(IInterfaceParent1), typeof(ConcreteParent1), typeof(IInterfaceParent1Base), typeof(ConcreteParentBase) }));
        }
    }

    [Test]
    public void Should_throw_an_actionable_exception_when_type_is_not_registered_and_not_a_message()
    {
        var registry = new MessageMetadataRegistry();
        registry.Initialize(new Conventions().IsMessageType, true);

        var exception = Assert.Throws<Exception>(() => registry.GetMessageMetadata(typeof(string)));

        Assert.That(exception?.Message, Does.Contain("Could not find metadata for 'System.String'").And.Contain("included in initial scanning").And.Contain("implements either 'IMessage', 'IEvent' or 'ICommand'"));
    }

    [Test]
    public void Should_return_null_when_type_identifier_loads_a_type_that_is_not_a_message()
    {
        var registry = new MessageMetadataRegistry();
        registry.Initialize(new Conventions().IsMessageType, true);

        var messageMetadata = registry.GetMessageMetadata(typeof(EndpointConfiguration).AssemblyQualifiedName);

        Assert.That(messageMetadata, Is.Null);
    }

    class MyEvent : ConcreteParent1, IInterfaceParent1;
    class MyOtherEvent : IMessage;
    class ConcreteParent1 : ConcreteParentBase;
    class ConcreteParentBase : IMessage;
    interface IInterfaceParent1 : IInterfaceParent1Base;
    interface IInterfaceParent1Base : IMessage;
}
