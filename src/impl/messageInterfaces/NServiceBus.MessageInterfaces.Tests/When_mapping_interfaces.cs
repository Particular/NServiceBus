using System;
using System.Reflection;
using NUnit.Framework;

namespace NServiceBus.MessageInterfaces.Tests
{
    [TestFixture]
    public class When_mapping_interfaces
    {
        IMessageMapper mapper;

        [SetUp]
        public void SetUp()
        {
            mapper = new MessageMapper.Reflection.MessageMapper();
        }

        [Test]
        public void Interfaces_with_only_properties_should_be_mapped()
        {
            mapper.Initialize(new[] { typeof(InterfaceWithProperties) });

            Assert.NotNull(mapper.GetMappedTypeFor(typeof(InterfaceWithProperties)));
        }
        [Test]
        public void Interfaces_with_methods_should_be_ignored()
        {
            mapper.Initialize(new[] {typeof(InterfaceWithMethods)});

            Assert.Null(mapper.GetMappedTypeFor(typeof(InterfaceWithMethods)));
        }
        
        [Test]
        public void Attributes_on_properties_should_be_mapped()
        {
            mapper.Initialize(new[]{typeof(InterfaceWithPropertiesAndAttributes)});
            Assert.IsTrue(PropertyContainsAttribute("SomeProperty",typeof(SomeAttribute),mapper.CreateInstance(typeof(InterfaceWithPropertiesAndAttributes))));
            
            // Doesn't affect properties without attributes
            Assert.IsFalse(PropertyContainsAttribute("SomeOtherProperty", typeof(SomeAttribute), mapper.CreateInstance(typeof(InterfaceWithPropertiesAndAttributes))));
        }

        private bool PropertyContainsAttribute(string propertyName, Type attributeType, object obj)
        {
            return obj.GetType().GetProperty(propertyName).GetCustomAttributes(attributeType,true).Length > 0;
        }
    }

    public interface InterfaceWithProperties
    {
        string SomeProperty { get; set; }
    }

    public interface InterfaceWithMethods
    {
        string SomeProperty { get; set; }
        void MethodOnInterface();
    }

    public interface InterfaceWithPropertiesAndAttributes
    {
        [SomeAttribute]
        string SomeProperty { get; set; }
        
        string SomeOtherProperty { get; set; }
    }

    public class SomeAttribute : Attribute
    {
        
    }
}