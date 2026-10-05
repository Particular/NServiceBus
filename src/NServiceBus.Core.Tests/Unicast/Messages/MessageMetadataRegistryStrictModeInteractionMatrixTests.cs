namespace NServiceBus.Unicast.Tests;

using System;
using NUnit.Framework;
using Unicast.Messages;

// Interaction matrix for strict registered-only mode x configured dynamic type loading.
//
// Strict mode is the stronger non-overridable policy: it forbids Type.GetType-based string loading and all runtime
// hierarchy inference/registration on cache misses. With strict off, DynamicTypeLoadingEnabled only controls
// unresolved string/header Type.GetType loading, while Type-based legacy hierarchy inference still works.
//
// | Strict | DynamicLoading | Registered string id | Unregistered string id | Type cache miss | Bare pre-registration | Generated-hierarchy pre-registration |
// |--------|----------------|----------------------|------------------------|-----------------|-----------------------|--------------------------------------|
// | off    | off            | resolve              | null (no Type.GetType) | register (inference) | register at init | register at init |
// | off    | on             | resolve              | Type.GetType + register | register (inference) | register at init | register at init |
// | on     | off            | resolve              | null (no load/register) | throw strict | throw strict at init | register at init |
// | on     | on             | resolve              | null (no load/register) | throw strict | throw strict at init | register at init |
[TestFixture]
public class MessageMetadataRegistryStrictModeInteractionMatrixTests
{
    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void Registered_string_identifier_resolves_in_all_modes(bool strictMode, bool dynamicTypeLoading)
    {
        var registry = CreateRegistry(strictMode, dynamicTypeLoading);
        registry.RegisterMessageTypeWithHierarchy(typeof(MyMessage), [typeof(IMessage)]);

        var metadata = registry.GetMessageMetadata(typeof(MyMessage).AssemblyQualifiedName);

        Assert.That(metadata.MessageType, Is.EqualTo(typeof(MyMessage)));
    }

    [TestCase(false, true)]
    public void Unregistered_string_identifier_loads_and_registers_when_dynamic_loading_enabled_and_strict_off(bool strictMode, bool dynamicTypeLoading)
    {
        var registry = CreateRegistry(strictMode, dynamicTypeLoading);
        registry.RegisterMessageTypeWithHierarchy(typeof(MyMessage), [typeof(IMessage)]);

        var metadata = registry.GetMessageMetadata(typeof(MyOtherMessage).AssemblyQualifiedName);

        Assert.That(metadata.MessageType, Is.EqualTo(typeof(MyOtherMessage)));
    }

    [TestCase(false, false)]
    public void Unregistered_string_identifier_returns_null_when_dynamic_loading_disabled_and_strict_off(bool strictMode, bool dynamicTypeLoading)
    {
        var registry = CreateRegistry(strictMode, dynamicTypeLoading);
        registry.RegisterMessageTypeWithHierarchy(typeof(MyMessage), [typeof(IMessage)]);

        var metadata = registry.GetMessageMetadata(typeof(MyOtherMessage).AssemblyQualifiedName);

        Assert.That(metadata, Is.Null);
    }

    [TestCase(true, false)]
    [TestCase(true, true)]
    public void Unregistered_string_identifier_returns_null_in_strict_mode(bool strictMode, bool dynamicTypeLoading)
    {
        var registry = CreateRegistry(strictMode, dynamicTypeLoading);
        registry.RegisterMessageTypeWithHierarchy(typeof(MyMessage), [typeof(IMessage)]);

        var metadata = registry.GetMessageMetadata(typeof(MyOtherMessage).AssemblyQualifiedName);

        Assert.That(metadata, Is.Null);
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    public void Type_cache_miss_registers_via_runtime_inference_when_strict_off(bool strictMode, bool dynamicTypeLoading)
    {
        var registry = CreateRegistry(strictMode, dynamicTypeLoading);
        registry.RegisterMessageTypeWithHierarchy(typeof(MyMessage), [typeof(IMessage)]);

        var metadata = registry.GetMessageMetadata(typeof(MyOtherMessage));

        Assert.That(metadata.MessageType, Is.EqualTo(typeof(MyOtherMessage)));
    }

    [TestCase(true, false)]
    [TestCase(true, true)]
    public void Type_cache_miss_throws_in_strict_mode(bool strictMode, bool dynamicTypeLoading)
    {
        var registry = CreateRegistry(strictMode, dynamicTypeLoading);
        registry.RegisterMessageTypeWithHierarchy(typeof(MyMessage), [typeof(IMessage)]);

        var exception = Assert.Throws<Exception>(() => registry.GetMessageMetadata(typeof(MyOtherMessage)));

        Assert.That(exception?.Message, Does.Contain("strict registered-only message metadata mode")
            .And.Contain("AddMessageType<TMessage>()")
            .And.Contain("AddHandler<T>()")
            .And.Contain("AddSaga<T>()"));
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    public void Bare_pre_registration_registers_at_initialization_when_strict_off(bool strictMode, bool dynamicTypeLoading)
    {
        var registry = new MessageMetadataRegistry { StrictRegisteredOnlyMode = strictMode };
        registry.RegisterMessageTypes([typeof(MyMessage)]);
        registry.Initialize(new Conventions().IsMessageType, dynamicTypeLoading);

        Assert.That(registry.GetMessageMetadata(typeof(MyMessage)).MessageType, Is.EqualTo(typeof(MyMessage)));
    }

    [TestCase(true, false)]
    [TestCase(true, true)]
    public void Bare_pre_registration_fails_at_initialization_in_strict_mode(bool strictMode, bool dynamicTypeLoading)
    {
        var registry = new MessageMetadataRegistry { StrictRegisteredOnlyMode = strictMode };
        registry.RegisterMessageTypes([typeof(MyMessage)]);

        var exception = Assert.Throws<Exception>(() => registry.Initialize(new Conventions().IsMessageType, dynamicTypeLoading));

        Assert.That(exception?.Message, Does.Contain("strict registered-only message metadata mode")
            .And.Contain("AddMessageType<TMessage>()")
            .And.Contain("AddHandler<T>()")
            .And.Contain("AddSaga<T>()"));
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void Generated_hierarchy_pre_registration_registers_at_initialization_in_all_modes(bool strictMode, bool dynamicTypeLoading)
    {
        var registry = new MessageMetadataRegistry { StrictRegisteredOnlyMode = strictMode };
        registry.RegisterMessageTypeWithHierarchy(typeof(MyMessage), [typeof(IMessage)]);
        registry.Initialize(new Conventions().IsMessageType, dynamicTypeLoading);

        Assert.That(registry.GetMessageMetadata(typeof(MyMessage)).MessageType, Is.EqualTo(typeof(MyMessage)));
    }

    static MessageMetadataRegistry CreateRegistry(bool strictMode, bool dynamicTypeLoading)
    {
        var registry = new MessageMetadataRegistry { StrictRegisteredOnlyMode = strictMode };
        registry.Initialize(new Conventions().IsMessageType, dynamicTypeLoading);
        return registry;
    }

    public class MyMessage : IMessage;
    public class MyOtherMessage : IMessage;
}
