namespace MessageMapperTests;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
using NUnit.Framework;

[TestFixture]
public class MessageMapperTests
{
    [Test]
    public void Initialize_ShouldBeThreadsafe()
    {
        var mapper = new MessageMapper();

        Parallel.For(0, 10, i =>
        {
            mapper.Initialize(new[]
            {
                typeof(SampleMessageClass),
                typeof(ISampleMessageInterface),
                typeof(ClassImplementingIEnumerable<>)
            });
        });
    }

    [Test]
    public void CreateInstance_WhenMessageInitialized_ShouldBeThreadsafe()
    {
        var mapper = new MessageMapper();

        mapper.Initialize(new[]
            {
                typeof(SampleMessageClass),
                typeof(ISampleMessageInterface),
                typeof(ClassImplementingIEnumerable<>)
            });

        Parallel.For(0, 10, i =>
        {
            mapper.CreateInstance<SampleMessageClass>();
            mapper.CreateInstance<ISampleMessageInterface>();
            mapper.CreateInstance<ClassImplementingIEnumerable<string>>();
        });
    }

#nullable enable
    [Test]
    public void Should_handle_messages_with_nullable_reference_types()
    {
        var mapper = new MessageMapper();

        mapper.CreateInstance<IMessageWithNullableProperties>();
    }

    public interface IMessageWithNullableProperties : ICommand, IMessage
    {
        string? NullableString { get; set; }
        object[]? NullableArray { get; set; }
        List<NullableComplexTypeItem>? NullableList { get; set; }
    }

    public class NullableComplexTypeItem
    {
    }
#nullable disable

    [Test]
    public void CreateInstance_WhenMessageNotInitialized_ShouldBeThreadsafe()
    {
        var mapper = new MessageMapper();

        Parallel.For(0, 10, i =>
        {
            mapper.CreateInstance<SampleMessageClass>();
            mapper.CreateInstance<ISampleMessageInterface>();
            mapper.CreateInstance<ClassImplementingIEnumerable<string>>();
        });
    }

    [Test]
    public void ShouldAllowMultipleMapperInstancesPerAppDomain()
    {
        Parallel.For(0, 10, i =>
        {
            var mapper = new MessageMapper();
            mapper.CreateInstance<SampleMessageClass>();
            mapper.CreateInstance<ISampleMessageInterface>();
            mapper.CreateInstance<ClassImplementingIEnumerable<string>>();
        });
    }

    [Test]
    public void Should_create_instance_of_concrete_type_with_illegal_interface_property()
    {
        var mapper = new MessageMapper();

        mapper.Initialize(new[] { typeof(ConcreteMessageWithIllegalInterfaceProperty) });

        mapper.CreateInstance<ConcreteMessageWithIllegalInterfaceProperty>();
    }

    [Test]
    public void Should_fail_for_interface_message_with_illegal_interface_property()
    {
        var mapper = new MessageMapper();

        var ex = Assert.Throws<Exception>(() => mapper.Initialize(new[] { typeof(IInterfaceMessageWithIllegalInterfaceProperty) }));
        Assert.That(ex.Message, Does.Contain($"Cannot generate a concrete implementation for '{typeof(IIllegalProperty).FullName}' because it contains methods. Ensure that all interfaces used as messages do not contain methods."));
    }

    [Test]
    public void Should_fail_for_non_public_interface_message()
    {
        var mapper = new MessageMapper();

        var ex = Assert.Throws<Exception>(() => mapper.Initialize(new[] { typeof(IPrivateInterfaceMessage) }));
        Assert.That(ex.Message, Does.Contain($"Cannot generate a concrete implementation for '{typeof(IPrivateInterfaceMessage).FullName}' because it is not public. Ensure that all interfaces used as messages are public."));
    }

    [Test]
    public void CreateInstance_should_initialize_interface_message_type_on_demand()
    {
        var mapper = new MessageMapper();

        var messageInstance = mapper.CreateInstance<ISampleMessageInterface>();

        Assert.That(messageInstance, Is.Not.Null);
        Assert.That(messageInstance, Is.InstanceOf<ISampleMessageInterface>());
    }

    [Test]
    public void CreateInstance_should_initialize_message_type_on_demand()
    {
        var mapper = new MessageMapper();

        var messageInstance = mapper.CreateInstance<SampleMessageClass>();

        Assert.That(messageInstance, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(messageInstance.GetType(), Is.EqualTo(typeof(SampleMessageClass)));
            Assert.That(messageInstance.CtorInvoked, Is.True);
        }
    }

    [Test]
    // this is not desired behavior and just documents current behavior
    public void CreateInstance_should_not_initialize_message_type_implementing_IEnumerable()
    {
        var mapper = new MessageMapper();

        var messageInstance = mapper.CreateInstance<ClassImplementingIEnumerable<string>>();

        Assert.That(messageInstance, Is.Not.Null);
        Assert.That(messageInstance.CtorInvoked, Is.False);
    }

    [Test]
    public void Should_create_structs()
    {
        var mapper = new MessageMapper();

        var messageInstance = mapper.CreateInstance<SampleMessageStruct>();

        Assert.That(messageInstance.GetType(), Is.EqualTo(typeof(SampleMessageStruct)));
    }

    [Test]
    public void Should_map_structs()
    {
        var mapper = new MessageMapper();

        var mappedType = mapper.GetMappedTypeFor(typeof(SampleMessageStruct));

        Assert.That(mappedType, Is.EqualTo(typeof(SampleMessageStruct)));
    }

    [Test]
    public void Should_handle_interfaces_that_have_attributes_with_nullable_properties()
    {
        var mapper = new MessageMapper();

        var messageInstance = mapper.CreateInstance<IMessageInterfaceWithNullableProperty>();

        Assert.That(messageInstance, Is.Not.Null);
    }

    public class SampleMessageClass
    {
        public SampleMessageClass()
        {
            CtorInvoked = true;
        }

        public bool CtorInvoked { get; }
    }

    public struct SampleMessageStruct
    {
    }

    public interface ISampleMessageInterface
    {
    }

    public class ClassImplementingIEnumerable<TItem> : IEnumerable<TItem>
    {
        public ClassImplementingIEnumerable()
        {
            CtorInvoked = true;
        }

        public bool CtorInvoked { get; }

        public IEnumerator<TItem> GetEnumerator()
        {
            return new List<TItem>.Enumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class ConcreteMessageWithIllegalInterfaceProperty
    {
        public IIllegalProperty MyProperty { get; set; }
    }

    public interface IInterfaceMessageWithIllegalInterfaceProperty
    {
        IIllegalProperty MyProperty { get; set; }
    }

    public interface IIllegalProperty
    {
        string SomeProperty { get; set; }

        //this is not supported by our mapper
        void SomeMethod();
    }

    interface IPrivateInterfaceMessage
    {
        string SomeValue { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class NullablePropertyAttribute : Attribute
    {
        public int? IntKey { get; set; }

        public NullablePropertyAttribute(int x)
        {
            IntKey = x;
        }
    }

    public interface IMessageInterfaceWithNullableProperty
    {
        [NullableProperty(0)]
        object Value { get; set; }
    }
}