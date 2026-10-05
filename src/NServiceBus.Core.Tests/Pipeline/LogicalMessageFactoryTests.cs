namespace NServiceBus.Core.Tests.Pipeline;

using System;
using System.Collections.Generic;
using MessageInterfaces;
using MessageInterfaces.MessageMapper.Reflection;
using NServiceBus.Pipeline;
using NUnit.Framework;
using Unicast.Messages;

[TestFixture]
public class LogicalMessageFactoryTests
{
    MessageMetadataRegistry registry;
    MessageMapper mapper;
    LogicalMessageFactory factory;

    [SetUp]
    public void SetUp()
    {
        registry = new MessageMetadataRegistry();
        registry.Initialize(new Conventions().IsMessageType, true);
        mapper = new MessageMapper();
        factory = new LogicalMessageFactory(registry, mapper);
    }

    [Test]
    public void Create_with_object_overload_uses_runtime_type_for_metadata()
    {
        var message = new ConcreteMessage();

        var logicalMessage = factory.Create(message);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(logicalMessage.MessageType, Is.EqualTo(typeof(ConcreteMessage)));
            Assert.That(logicalMessage.Instance, Is.SameAs(message));
        }
    }

    [Test]
    public void Create_with_explicit_concrete_type_uses_that_type_for_metadata()
    {
        var message = new ConcreteMessage();

        var logicalMessage = factory.Create(typeof(ConcreteMessage), message);

        Assert.That(logicalMessage.MessageType, Is.EqualTo(typeof(ConcreteMessage)));
    }

    [Test]
    public void Create_normalizes_proxy_instances_to_the_interface_type()
    {
        mapper.Initialize([typeof(IMyMessage)]);
        var proxy = mapper.CreateInstance<IMyMessage>();

        var logicalMessage = factory.Create(proxy.GetType(), proxy);

        Assert.That(logicalMessage.MessageType, Is.EqualTo(typeof(IMyMessage)));
    }

    [Test]
    public void Create_with_interface_type_resolves_metadata_for_the_generated_proxy_type()
    {
        mapper.Initialize([typeof(IMyMessage)]);
        var proxy = mapper.CreateInstance<IMyMessage>();

        var logicalMessage = factory.Create(typeof(IMyMessage), proxy);

        Assert.That(logicalMessage.MessageType, Is.EqualTo(proxy.GetType()));
    }

    [Test]
    public void Create_throws_not_supported_when_mapper_cannot_map_interface_type()
    {
        var trimmingSafeFactory = new LogicalMessageFactory(registry, new TrimmingSafeMessageMapper());

        Assert.Throws<NotSupportedException>(() => trimmingSafeFactory.Create(typeof(IMyMessage), new ConcreteMessage()));
    }

    [Test]
    public void Create_with_metadata_overload_uses_given_metadata_without_invoking_mapper()
    {
        var throwingMapper = new ThrowingMessageMapper();
        var throwingFactory = new LogicalMessageFactory(registry, throwingMapper);
        var metadata = registry.GetMessageMetadata(typeof(ConcreteMessage));
        var message = new ConcreteMessage();

        var logicalMessage = throwingFactory.Create(metadata, message);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(logicalMessage.MessageType, Is.EqualTo(typeof(ConcreteMessage)));
            Assert.That(logicalMessage.Metadata, Is.SameAs(metadata));
            Assert.That(logicalMessage.Instance, Is.SameAs(message));
        }
    }

    [Test]
    public void Create_with_metadata_overload_throws_when_metadata_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => factory.Create((MessageMetadata)null, new ConcreteMessage()));
    }

    [Test]
    public void Create_throws_when_type_has_no_metadata_and_is_not_a_message_type()
    {
        Assert.Throws<Exception>(() => factory.Create(typeof(string), "not a message"));
    }

    public interface IMyMessage : IMessage
    {
        string SomeProperty { get; set; }
    }

    public class ConcreteMessage : IMessage
    {
        public string SomeProperty { get; set; }
    }

    class ThrowingMessageMapper : IMessageMapper
    {
        public void Initialize(IEnumerable<Type> types) => throw new InvalidOperationException("Should not be called");

        public Type GetMappedTypeFor(Type t) => throw new InvalidOperationException("Should not be called");

        public Type GetMappedTypeFor(string typeName) => throw new InvalidOperationException("Should not be called");

        public T CreateInstance<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(IMessageCreator.CreatorMembersRequired)] T>() => throw new InvalidOperationException("Should not be called");

        public T CreateInstance<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(IMessageCreator.CreatorMembersRequired)] T>(Action<T> action) => throw new InvalidOperationException("Should not be called");

        public object CreateInstance([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(IMessageCreator.CreatorMembersRequired)] Type t) => throw new InvalidOperationException("Should not be called");
    }
}
