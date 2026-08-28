namespace NServiceBus.Unicast.Tests;

using System;
using NUnit.Framework;
using Unicast.Messages;

[TestFixture]
public class MessageMetadataRegistryStrictModeTests
{
    [Test]
    public void Should_throw_actionable_exception_for_unregistered_message_type()
    {
        var registry = CreateStrictRegistry();

        var exception = Assert.Throws<Exception>(() => registry.GetMessageMetadata(typeof(MyOtherMessage)));

        Assert.That(exception?.Message, Does.Contain("strict registered-only message metadata mode")
            .And.Contain("AddMessageType<TMessage>()")
            .And.Contain("AddHandler<T>()")
            .And.Contain("AddSaga<T>()"));
    }

    [Test]
    public void Should_resolve_registered_message_type_in_strict_mode()
    {
        var registry = CreateStrictRegistry();

        var metadata = registry.GetMessageMetadata(typeof(MyMessage));

        Assert.That(metadata.MessageType, Is.EqualTo(typeof(MyMessage)));
    }

    [Test]
    public void Should_register_generated_hierarchy_pre_registration_during_initialization_in_strict_mode()
    {
        var registry = new MessageMetadataRegistry { StrictRegisteredOnlyMode = true };
        registry.RegisterMessageTypeWithHierarchy(typeof(MyMessage), [typeof(IMessage)]);
        registry.Initialize(new Conventions().IsMessageType, true);

        Assert.That(registry.GetMessageMetadata(typeof(MyMessage)).MessageType, Is.EqualTo(typeof(MyMessage)));
    }

    [Test]
    public void Should_fail_bare_pre_registration_during_initialization_in_strict_mode()
    {
        var registry = new MessageMetadataRegistry { StrictRegisteredOnlyMode = true };
        registry.RegisterMessageTypes([typeof(MyMessage)]);

        var exception = Assert.Throws<Exception>(() => registry.Initialize(new Conventions().IsMessageType, true));

        Assert.That(exception?.Message, Does.Contain("strict registered-only message metadata mode")
            .And.Contain("AddMessageType<TMessage>()")
            .And.Contain("AddHandler<T>()")
            .And.Contain("AddSaga<T>()"));
    }

    [Test]
    public void Should_return_null_for_unknown_identifier_in_strict_mode_without_dynamic_loading()
    {
        var registry = CreateStrictRegistry();

        // The type loads from a real assembly, but strict mode must not resolve or register it.
        var metadata = registry.GetMessageMetadata(typeof(EndpointConfiguration).AssemblyQualifiedName);

        Assert.That(metadata, Is.Null);
    }

    [Test]
    public void Should_return_metadata_for_registered_identifier_in_strict_mode()
    {
        var registry = CreateStrictRegistry();

        var metadata = registry.GetMessageMetadata(typeof(MyMessage).AssemblyQualifiedName);

        Assert.That(metadata.MessageType, Is.EqualTo(typeof(MyMessage)));
    }

    static MessageMetadataRegistry CreateStrictRegistry()
    {
        var registry = new MessageMetadataRegistry { StrictRegisteredOnlyMode = true };
        registry.RegisterMessageTypeWithHierarchy(typeof(MyMessage), [typeof(IMessage)]);
        registry.Initialize(new Conventions().IsMessageType, true);
        return registry;
    }

    public class MyMessage : IMessage;
    public class MyOtherMessage : IMessage;
}
