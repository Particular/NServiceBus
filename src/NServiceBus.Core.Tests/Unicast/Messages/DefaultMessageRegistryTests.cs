namespace NServiceBus.Unicast.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unicast.Messages;

[TestFixture]
public class MessageMetadataRegistryTests
{
    [Test]
    public void Should_throw_an_exception_for_a_unmapped_type()
    {
        var defaultMessageRegistry = new MessageMetadataRegistry(_ => false, true);
        Assert.Throws<Exception>(() => defaultMessageRegistry.GetMessageMetadata(typeof(int)));
    }

    [Test]
    public void Should_return_null_when_resolving_unknown_type_from_type_identifier()
    {
        var registry = new MessageMetadataRegistry(t => true, true);

        var metadata = registry.GetMessageMetadata("SomeNamespace.SomeType, SomeAssemblyName, Version=81.0.0.0, Culture=neutral, PublicKeyToken=null");

        Assert.IsNull(metadata);
    }

    [Test]
    public void Should_return_metadata_for_a_mapped_type()
    {
        var defaultMessageRegistry = new MessageMetadataRegistry(type => type == typeof(int), true);
        defaultMessageRegistry.RegisterMessageTypesFoundIn([typeof(int)]);

        var messageMetadata = defaultMessageRegistry.GetMessageMetadata(typeof(int));

        Assert.That(messageMetadata.MessageType, Is.EqualTo(typeof(int)));
        Assert.That(messageMetadata.MessageHierarchy.Length, Is.EqualTo(1));
    }


    [Test]
    public void Should_return_the_correct_parent_hierarchy()
    {
        var defaultMessageRegistry = new MessageMetadataRegistry(new Conventions().IsMessageType, true);

        defaultMessageRegistry.RegisterMessageTypesFoundIn([typeof(MyEvent)]);
        var messageMetadata = defaultMessageRegistry.GetMessageMetadata(typeof(MyEvent));

        Assert.That(messageMetadata.MessageHierarchy.Length, Is.EqualTo(5));

        Assert.That(messageMetadata.MessageHierarchy.ToList()[0], Is.EqualTo(typeof(MyEvent)));
        Assert.That(messageMetadata.MessageHierarchy.ToList()[1], Is.EqualTo(typeof(IInterfaceParent1)));
        Assert.That(messageMetadata.MessageHierarchy.ToList()[2], Is.EqualTo(typeof(ConcreteParent1)));
        Assert.That(messageMetadata.MessageHierarchy.ToList()[3], Is.EqualTo(typeof(IInterfaceParent1Base)));
        Assert.That(messageMetadata.MessageHierarchy.ToList()[4], Is.EqualTo(typeof(ConcreteParentBase)));
    }

    [TestCase("NServiceBus.Unicast.Tests.MessageMetadataRegistryTests+MyEvent, NonExistingAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b50674d1e0c6ce54")]
    [TestCase("NServiceBus.Unicast.Tests.MessageMetadataRegistryTests+MyEvent, NonExistingAssembly, Version=1.0.0.0, Culture=neutral")]
    [TestCase("NServiceBus.Unicast.Tests.MessageMetadataRegistryTests+MyEvent, NonExistingAssembly")]
    [TestCase("NServiceBus.Unicast.Tests.MessageMetadataRegistryTests+MyEvent")]
    public void Should_match_types_from_a_different_assembly(string typeName)
    {
        var defaultMessageRegistry = new MessageMetadataRegistry(new Conventions().IsMessageType, true);
        defaultMessageRegistry.RegisterMessageTypesFoundIn([typeof(MyEvent)]);

        var messageMetadata = defaultMessageRegistry.GetMessageMetadata(typeName);

        Assert.That(messageMetadata.MessageHierarchy.ToList()[0], Is.EqualTo(typeof(MyEvent)));
    }

    [Test]
    public void Should_not_match_same_type_names_with_different_namespace()
    {
        var defaultMessageRegistry = new MessageMetadataRegistry(new Conventions().IsMessageType, true);
        defaultMessageRegistry.RegisterMessageTypesFoundIn([typeof(MyEvent)]);

        string typeIdentifier = typeof(MyEvent).AssemblyQualifiedName.Replace(typeof(MyEvent).FullName, $"SomeNamespace.{nameof(MyEvent)}");
        var messageMetadata = defaultMessageRegistry.GetMessageMetadata(typeIdentifier);

        Assert.IsNull(messageMetadata);
    }

    [Test]
    public void Should_resolve_uninitialized_types_from_loaded_assemblies()
    {
        var registry = new MessageMetadataRegistry(t => true, true);

        var metadata = registry.GetMessageMetadata(typeof(EndpointConfiguration).AssemblyQualifiedName);

        Assert.That(metadata.MessageType, Is.EqualTo(typeof(EndpointConfiguration)));
    }

    [Test]
    public void Should_not_resolve_uninitialized_types_from_assembly_when_prohibiting_dynamic_typeloading()
    {
        var registry = new MessageMetadataRegistry(t => true, false);

        var metadata = registry.GetMessageMetadata(typeof(EndpointConfiguration).AssemblyQualifiedName);

        Assert.IsNull(metadata);
    }

    class MyEvent : ConcreteParent1, IInterfaceParent1
    {

    }

    class ConcreteParent1 : ConcreteParentBase
    {

    }
    class ConcreteParentBase : IMessage
    {

    }

    interface IInterfaceParent1 : IInterfaceParent1Base
    {

    }

    interface IInterfaceParent1Base : IMessage
    {

    }
}