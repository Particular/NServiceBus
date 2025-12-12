namespace NServiceBus.Unicast.Tests;

using System;
using System.Linq;
using NUnit.Framework;
using Unicast.Messages;

[TestFixture]
public class MessageMetadataRegistryTests
{
    [Test]
    public void Should_throw_an_exception_when_not_initialized()
    {
        var registry = new MessageMetadataRegistry();

        Assert.Throws<InvalidOperationException>(() => registry.GetMessageMetadata(typeof(int)));
    }

    [Test]
    public void Should_not_throw_an_exception_for_register_when_not_initialized()
    {
        var registry = new MessageMetadataRegistry();

        Assert.DoesNotThrow(() => registry.RegisterMessageTypeWithHierarchy(typeof(object), []));
    }

    [Test]
    public void Should_throw_an_exception_for_a_unmapped_type()
    {
        var registry = new MessageMetadataRegistry();
        registry.Initialize(_ => false, true);

        Assert.Throws<Exception>(() => registry.GetMessageMetadata(typeof(int)));
    }

    [Test]
    public void Should_return_null_when_resolving_unknown_type_from_type_identifier()
    {
        var registry = new MessageMetadataRegistry();
        registry.Initialize(t => true, true);

        var metadata = registry.GetMessageMetadata("SomeNamespace.SomeType, SomeAssemblyName, Version=81.0.0.0, Culture=neutral, PublicKeyToken=null");

        Assert.That(metadata, Is.Null);
    }

    [Test]
    public void Should_return_metadata_for_a_mapped_type()
    {
        var registry = new MessageMetadataRegistry();
        registry.Initialize(type => type == typeof(int), true);
        registry.RegisterMessageTypes([typeof(int)]);

        var messageMetadata = registry.GetMessageMetadata(typeof(int));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(messageMetadata.MessageType, Is.EqualTo(typeof(int)));
            Assert.That(messageMetadata.MessageHierarchy, Has.Length.EqualTo(1));
        }
    }

    [Test]
    public void Should_return_the_correct_parent_hierarchy()
    {
        var registry = new MessageMetadataRegistry();
        registry.Initialize(new Conventions().IsMessageType, true);

        registry.RegisterMessageTypes([typeof(MyEvent)]);
        var messageMetadata = registry.GetMessageMetadata(typeof(MyEvent));

        Assert.That(messageMetadata.MessageHierarchy, Has.Length.EqualTo(5));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(messageMetadata.MessageHierarchy.ToList()[0], Is.EqualTo(typeof(MyEvent)));
            Assert.That(messageMetadata.MessageHierarchy.ToList()[1], Is.EqualTo(typeof(IInterfaceParent1)));
            Assert.That(messageMetadata.MessageHierarchy.ToList()[2], Is.EqualTo(typeof(ConcreteParent1)));
            Assert.That(messageMetadata.MessageHierarchy.ToList()[3], Is.EqualTo(typeof(IInterfaceParent1Base)));
            Assert.That(messageMetadata.MessageHierarchy.ToList()[4], Is.EqualTo(typeof(ConcreteParentBase)));
        }
    }

    [TestCase("NServiceBus.Unicast.Tests.MessageMetadataRegistryTests+MyEvent, NonExistingAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b50674d1e0c6ce54")]
    [TestCase("NServiceBus.Unicast.Tests.MessageMetadataRegistryTests+MyEvent, NonExistingAssembly, Version=1.0.0.0, Culture=neutral")]
    [TestCase("NServiceBus.Unicast.Tests.MessageMetadataRegistryTests+MyEvent, NonExistingAssembly")]
    [TestCase("NServiceBus.Unicast.Tests.MessageMetadataRegistryTests+MyEvent")]
    public void Should_match_types_from_a_different_assembly(string typeName)
    {
        var registry = new MessageMetadataRegistry();
        registry.Initialize(new Conventions().IsMessageType, true);
        registry.RegisterMessageTypes([typeof(MyEvent)]);

        var messageMetadata = registry.GetMessageMetadata(typeName);

        Assert.That(messageMetadata.MessageHierarchy.ToList()[0], Is.EqualTo(typeof(MyEvent)));
    }

    [Test]
    public void Should_not_match_same_type_names_with_different_namespace()
    {
        var registry = new MessageMetadataRegistry();
        registry.Initialize(new Conventions().IsMessageType, true);
        registry.RegisterMessageTypes([typeof(MyEvent)]);

        string typeIdentifier = typeof(MyEvent).AssemblyQualifiedName.Replace(typeof(MyEvent).FullName, $"SomeNamespace.{nameof(MyEvent)}");
        var messageMetadata = registry.GetMessageMetadata(typeIdentifier);

        Assert.That(messageMetadata, Is.Null);
    }

    [Test]
    public void Should_resolve_uninitialized_types_from_loaded_assemblies()
    {
        var registry = new MessageMetadataRegistry();
        registry.Initialize(t => true, true);

        var metadata = registry.GetMessageMetadata(typeof(EndpointConfiguration).AssemblyQualifiedName);

        Assert.That(metadata.MessageType, Is.EqualTo(typeof(EndpointConfiguration)));
    }

    [Test]
    public void Should_not_resolve_uninitialized_types_from_assembly_when_prohibiting_dynamic_typeloading()
    {
        var registry = new MessageMetadataRegistry();
        registry.Initialize(t => true, false);

        var metadata = registry.GetMessageMetadata(typeof(EndpointConfiguration).AssemblyQualifiedName);

        Assert.That(metadata, Is.Null);
    }

    class MyEvent : ConcreteParent1, IInterfaceParent1;
    class ConcreteParent1 : ConcreteParentBase;
    class ConcreteParentBase : IMessage;
    interface IInterfaceParent1 : IInterfaceParent1Base;
    interface IInterfaceParent1Base : IMessage;
}