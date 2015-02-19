namespace MessageMapperTests
{
    using System;
    using NServiceBus;
    using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
    using NUnit.Framework;

    [TestFixture]
    public class When_mapping_interfaces
    {
        [Test]
        public void Interfaces_with_only_properties_should_be_mapped()
        {
            var mapper = new MessageMapper();
            mapper.Initialize(new[] { typeof(InterfaceWithOnlyProperties) });

            Assert.NotNull(mapper.GetMappedTypeFor(typeof(InterfaceWithOnlyProperties)));
        }

        public interface InterfaceWithOnlyProperties : IMessage
        {
            string SomeProperty { get; set; }
        }

        [Test]
        public void Interface_should_be_created()
        {
            var mapper = new MessageMapper();
            mapper.Initialize(new[] { typeof(Interface) });

            var result = mapper.CreateInstance<Interface>(null);

            Assert.IsNotNull(result);
        }

        public interface Interface : IMessage
        {
        }

        [Test]
        public void Interfaces_with_methods_are_not_supported()
        {
            var mapper = new MessageMapper();
            Assert.Throws<Exception>(() => mapper.Initialize(new[]
            {
                typeof(InterfaceWithMethods)
            }));
        }

        public interface InterfaceWithMethods
        {
            string SomeProperty { get; set; }
            void MethodOnInterface();
        }

        
        [Test]
        public void Attributes_on_properties_should_be_mapped()
        {
            var mapper = new MessageMapper();
            mapper.Initialize(new[]{typeof(InterfaceWithPropertiesAndAttributes)});
            Assert.IsTrue(PropertyContainsAttribute("SomeProperty",typeof(SomeAttribute),mapper.CreateInstance(typeof(InterfaceWithPropertiesAndAttributes))));
            
            // Doesn't affect properties without attributes
            Assert.IsFalse(PropertyContainsAttribute("SomeOtherProperty", typeof(SomeAttribute), mapper.CreateInstance(typeof(InterfaceWithPropertiesAndAttributes))));
        }

        public interface InterfaceWithPropertiesAndAttributes
        {
            [Some]
            string SomeProperty { get; set; }
            string SomeOtherProperty { get; set; }
        }

        public class SomeAttribute : Attribute
        {
        }


        [Test]
        public void Accept_Attributes_with_no_default_ctor_as_long_as_the_parameter_in_constructor_has_the_same_name_as_the_property()
        {
            var mapper = new MessageMapper();
            mapper.Initialize(new[] { typeof(InterfaceWithCustomAttributeThatHasNoDefaultConstructor) });
            var instance = mapper.CreateInstance(typeof (InterfaceWithCustomAttributeThatHasNoDefaultConstructor));
            var attributes = instance.GetType().GetProperty("SomeProperty").GetCustomAttributes(typeof(CustomAttributeWithNoDefaultConstructor),true);
            var attr = (CustomAttributeWithNoDefaultConstructor)attributes[0];
            Assert.AreEqual(attr.Name, "Blah");
        }

        public interface InterfaceWithCustomAttributeThatHasNoDefaultConstructor
        {
            [CustomAttributeWithNoDefaultConstructor("Blah")]
            string SomeProperty { get; set; }
        }

        public class CustomAttributeWithNoDefaultConstructor : Attribute
        {
            public string Name { get; set; }
            public CustomAttributeWithNoDefaultConstructor(string name)
            {
                Name = name;
            }
        }

        [Test]
        public void Accept_Attributes_with_no_default_ctor_while_ctor_parameters_are_different_than_properties_of_custom_attribute()
        {
            var mapper = new MessageMapper();
            mapper.Initialize(new[] { typeof(InterfaceWithCustomAttributeThatHasNoDefaultConstructorAndNoMatchingParameters) });
            var instance = mapper.CreateInstance(typeof(InterfaceWithCustomAttributeThatHasNoDefaultConstructorAndNoMatchingParameters));
            var attributes = instance.GetType().GetProperty("SomeProperty").GetCustomAttributes(typeof(CustomAttributeWithNoDefaultConstructorAndNoMatchingParameters), true);
            var attr = (CustomAttributeWithNoDefaultConstructorAndNoMatchingParameters)attributes[0];
            Assert.AreEqual(attr.Name, "Blah");
        }
        public interface InterfaceWithCustomAttributeThatHasNoDefaultConstructorAndNoMatchingParameters
        {
            [CustomAttributeWithNoDefaultConstructorAndNoMatchingParameters("Blah", "Second Blah")]
            string SomeProperty { get; set; }
        }


        /// <summary>
        /// Break the heuristics of finding the properties from constructor parameter names
        /// </summary>
        public class CustomAttributeWithNoDefaultConstructorAndNoMatchingParameters : Attribute
        {
            public string Name { get; set; }
            // ReSharper disable once UnusedParameter.Local
            public CustomAttributeWithNoDefaultConstructorAndNoMatchingParameters(string someParam, string someOtherParam)
            {
                Name = someParam;
            }
        }

        [Test]
        public void Generated_type_should_preserve_namespace_to_make_it_easier_for_users_to_define_custom_conventions()
        {
            var mapper = new MessageMapper();
            mapper.Initialize(new[] { typeof(InterfaceToGenerate) });

            Assert.AreEqual(typeof(InterfaceToGenerate).Namespace, mapper.CreateInstance(typeof(InterfaceToGenerate)).GetType().Namespace);
        }

        public interface InterfaceToGenerate : IMessage
        {
            string SomeProperty { get; set; }
        }

        [Test]
        public void Accept_attributes_with_value_attribute()
        {
            var mapper = new MessageMapper();
            mapper.Initialize(new[] { typeof(IMyEventWithAttributeWithBoolProperty) });
            var instance = mapper.CreateInstance(typeof(IMyEventWithAttributeWithBoolProperty));
            var attributes = instance.GetType().GetProperty("EventId").GetCustomAttributes(typeof(CustomAttributeWithValueProperties), true);
            var attr = attributes[0] as CustomAttributeWithValueProperties;
            Assert.AreEqual(attr != null && attr.FlagIsSet, true);
            if (attr != null) Assert.AreEqual(attr.MyAge, 21);
        }

        public interface IMyEventWithAttributeWithBoolProperty
        {
            [CustomAttributeWithValueProperties("bla bla", true, 21)]
            Guid EventId { get; set; }
        }
        public class CustomAttributeWithValueProperties : Attribute
        {
            public CustomAttributeWithValueProperties() { }

            public CustomAttributeWithValueProperties(string name, bool flag, int age)
            {
                Name = name;
                FlagIsSet = flag;
                MyAge = age;
            }

            public string Name { get; set; }

            public bool FlagIsSet { get; set; }

            public int MyAge { get; set; }
        }
        
        private bool PropertyContainsAttribute(string propertyName, Type attributeType, object obj)
        {
            return obj.GetType().GetProperty(propertyName).GetCustomAttributes(attributeType,true).Length > 0;
        }
    }




    
}
