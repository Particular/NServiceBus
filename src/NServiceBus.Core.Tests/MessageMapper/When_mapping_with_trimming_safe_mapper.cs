#nullable enable

namespace MessageMapperTests;

using System;
using NServiceBus;
using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
using NUnit.Framework;

[TestFixture]
public class When_mapping_with_trimming_safe_mapper
{
    [Test]
    public void Initialize_is_a_noop_and_does_not_throw()
    {
        var mapper = new TrimmingSafeMessageMapper();

        Assert.DoesNotThrow(() => mapper.Initialize(null));
        Assert.DoesNotThrow(() => mapper.Initialize([]));
        Assert.DoesNotThrow(() => mapper.Initialize([typeof(IInterfaceWithOnlyProperties)]));
    }

    [Test]
    public void GetMappedTypeFor_returns_the_same_concrete_type()
    {
        var mapper = new TrimmingSafeMessageMapper();

        Assert.That(mapper.GetMappedTypeFor(typeof(ConcreteMessage)), Is.SameAs(typeof(ConcreteMessage)));
    }

    [Test]
    public void GetMappedTypeFor_returns_the_same_interface_type_unchanged()
    {
        var mapper = new TrimmingSafeMessageMapper();

        // No mapping is available without dynamic code; the type is returned as-is.
        Assert.That(mapper.GetMappedTypeFor(typeof(IInterfaceWithOnlyProperties)), Is.SameAs(typeof(IInterfaceWithOnlyProperties)));
    }

    [Test]
    public void GetMappedTypeFor_by_name_returns_null()
    {
        var mapper = new TrimmingSafeMessageMapper();

        Assert.That(mapper.GetMappedTypeFor(typeof(ConcreteMessage).FullName!), Is.Null);
    }

    [Test]
    public void CreateInstance_for_concrete_type_returns_an_instance()
    {
        var mapper = new TrimmingSafeMessageMapper();

        var instance = mapper.CreateInstance(typeof(ConcreteMessage));

        Assert.That(instance, Is.InstanceOf<ConcreteMessage>());
    }

    [Test]
    public void CreateInstance_generic_for_concrete_type_returns_an_instance()
    {
        var mapper = new TrimmingSafeMessageMapper();

        var instance = mapper.CreateInstance<ConcreteMessage>();

        Assert.That(instance, Is.InstanceOf<ConcreteMessage>());
    }

    [Test]
    public void CreateInstance_generic_with_action_applies_the_action()
    {
        var mapper = new TrimmingSafeMessageMapper();

        var instance = mapper.CreateInstance<ConcreteMessage>(m => m.SomeProperty = "hello");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(instance, Is.InstanceOf<ConcreteMessage>());
            Assert.That(instance.SomeProperty, Is.EqualTo("hello"));
        }
    }

    [Test]
    public void CreateInstance_for_interface_throws_not_supported()
    {
        var mapper = new TrimmingSafeMessageMapper();

        var ex = Assert.Throws<NotSupportedException>(() => mapper.CreateInstance(typeof(IInterfaceWithOnlyProperties)));

        Assert.That(ex!.Message, Does.Contain("not supported").And.Contain("dynamic code"));
    }

    [Test]
    public void CreateInstance_generic_for_interface_throws_not_supported()
    {
        var mapper = new TrimmingSafeMessageMapper();

        Assert.Throws<NotSupportedException>(() => mapper.CreateInstance<IInterfaceWithOnlyProperties>());
    }

    [Test]
    public void CreateInstance_for_abstract_type_throws_not_supported()
    {
        var mapper = new TrimmingSafeMessageMapper();

        Assert.Throws<NotSupportedException>(() => mapper.CreateInstance(typeof(AbstractMessage)));
    }

    public interface IInterfaceWithOnlyProperties : IMessage
    {
        string SomeProperty { get; set; }
    }

    public class ConcreteMessage : IMessage
    {
        public string? SomeProperty { get; set; }
    }

    public abstract class AbstractMessage
    {
        public string? SomeProperty { get; set; }
    }
}