namespace NServiceBus.MessageInterfaces.Tests
{
    using System;
    using NUnit.Framework;

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
        public void Interface_should_be_created()
        {
            mapper.Initialize(new[] { typeof(InterfaceWithProperties) });

            var result = mapper.CreateInstance<InterfaceWithProperties>(null);

            Assert.IsNotNull(result);
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

        [Test]
        public void Accept_Attributes_with_no_default_ctor_as_long_as_the_parameter_in_constructor_has_the_same_name_as_the_property()
        {
            mapper.Initialize(new[] { typeof(InterfaceWithCustomAttributeThatHasNoDefaultConstructor) });
            var instance = mapper.CreateInstance(typeof (InterfaceWithCustomAttributeThatHasNoDefaultConstructor));
            var attributes = instance.GetType().GetProperty("SomeProperty").GetCustomAttributes(typeof(CustomAttributeWithNoDefaultConstructor),true);
            var attr = attributes[0] as CustomAttributeWithNoDefaultConstructor;
            Assert.AreEqual(attr.Name, "Blah");
        }


        [Test]
        public void Accept_Attributes_with_no_default_ctor_while_ctor_parameters_are_different_than_properties_of_custom_attribute()
        {
            mapper.Initialize(new[] { typeof(InterfaceWithCustomAttributeThatHasNoDefaultConstructorAndNoMatchingParameters) });
            var instance = mapper.CreateInstance(typeof(InterfaceWithCustomAttributeThatHasNoDefaultConstructorAndNoMatchingParameters));
            var attributes = instance.GetType().GetProperty("SomeProperty").GetCustomAttributes(typeof(CustomAttributeWithNoDefaultConstructorAndNoMatchingParameters), true);
            var attr = attributes[0] as CustomAttributeWithNoDefaultConstructorAndNoMatchingParameters;
            Assert.AreEqual(attr.Name, "Blah");
        }


        [Test]
        public void Generated_type_should_preserve_namespace_to_make_it_easier_for_users_to_define_custom_conventions()
        {
            mapper.Initialize(new[] { typeof(InterfaceWithProperties) });

            Assert.AreEqual(typeof(InterfaceWithProperties).Namespace, mapper.CreateInstance(typeof(InterfaceWithProperties)).GetType().Namespace);
        }

        [Test]
        public void Accept_attributes_with_value_attribute()
        {
            mapper.Initialize(new[] { typeof(IMyEventWithAttributeWithBoolProperty) });
            var instance = mapper.CreateInstance(typeof(IMyEventWithAttributeWithBoolProperty));
            var attributes = instance.GetType().GetProperty("EventId").GetCustomAttributes(typeof(CustomAttributeWithValueProperties), true);
            var attr = attributes[0] as CustomAttributeWithValueProperties;
            Assert.AreEqual(attr != null && attr.FlagIsSet, true);
            if (attr != null) Assert.AreEqual(attr.MyAge, 21);
        }
        
        private bool PropertyContainsAttribute(string propertyName, Type attributeType, object obj)
        {
            return obj.GetType().GetProperty(propertyName).GetCustomAttributes(attributeType,true).Length > 0;
        }
    }

    public interface InterfaceWithProperties : IMessage
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
        [Some]
        string SomeProperty { get; set; }
        
        string SomeOtherProperty { get; set; }
    }

    public interface InterfaceWithCustomAttributeThatHasNoDefaultConstructor
    {
        [CustomAttributeWithNoDefaultConstructor("Blah")]
        string SomeProperty { get; set; }
    }

    public class SomeAttribute : Attribute
    {
        
    }

    public class CustomAttributeWithNoDefaultConstructor : Attribute
    {
        public string Name { get; set; }
        public CustomAttributeWithNoDefaultConstructor(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Break the heuristics of finding the properties from constructor parameter names
    /// </summary>
    public class CustomAttributeWithNoDefaultConstructorAndNoMatchingParameters : Attribute
    {
        public string Name { get; set; }
        public CustomAttributeWithNoDefaultConstructorAndNoMatchingParameters(string someParam, string someOtherParam)
        {
            Name = someParam;
        }
    }
    public interface InterfaceWithCustomAttributeThatHasNoDefaultConstructorAndNoMatchingParameters
    {
        [CustomAttributeWithNoDefaultConstructorAndNoMatchingParameters("Blah", "Second Blah")]
        string SomeProperty { get; set; }
    }
    
    public class CustomAttributeWithValueProperties : Attribute
    {
        private string name;
        private bool flag;
        private int age;

        public CustomAttributeWithValueProperties(){}

        public CustomAttributeWithValueProperties(string name, bool flag, int age)
        {
            this.name = name;
            this.flag = flag;
            this.age = age;
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public bool FlagIsSet
        {
            get { return flag; }
            set { flag = value; }
        }
        public int MyAge
        {
            get { return age; }
            set { age = value; }
        }

    }
    public interface IMyEventWithAttributeWithBoolProperty
    {
        [CustomAttributeWithValueProperties("bla bla", true, 21)]
        Guid EventId { get; set; }
    }
}
