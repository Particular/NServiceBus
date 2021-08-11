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
            mapper.Initialize(new[] { typeof(IInterfaceWithOnlyProperties) });

            Assert.NotNull(mapper.GetMappedTypeFor(typeof(IInterfaceWithOnlyProperties)));
        }

        public interface IInterfaceWithOnlyProperties : IMessage
        {
            string SomeProperty { get; set; }
        }

        [Test]
        public void Interface_should_be_created()
        {
            var mapper = new MessageMapper();
            mapper.Initialize(new[] { typeof(IInterface) });

            var result = mapper.CreateInstance<IInterface>(null);

            Assert.IsNotNull(result);
        }

        public interface IInterface : IMessage
        {
        }

        [Test]
        public void Interfaces_with_methods_are_not_supported()
        {
            var mapper = new MessageMapper();
            Assert.Throws<Exception>(() => mapper.Initialize(new[]
            {
                typeof(IInterfaceWithMethods)
            }));
        }

        public interface IInterfaceWithMethods
        {
            string SomeProperty { get; set; }
            void MethodOnInterface();
        }


        [Test]
        public void Attributes_on_properties_should_be_mapped()
        {
            var mapper = new MessageMapper();
            mapper.Initialize(new[] { typeof(IInterfaceWithPropertiesAndAttributes) });
            Assert.IsTrue(PropertyContainsAttribute("SomeProperty", typeof(SomeAttribute), mapper.CreateInstance(typeof(IInterfaceWithPropertiesAndAttributes))));

            // Doesn't affect properties without attributes
            Assert.IsFalse(PropertyContainsAttribute("SomeOtherProperty", typeof(SomeAttribute), mapper.CreateInstance(typeof(IInterfaceWithPropertiesAndAttributes))));
        }

        public interface IInterfaceWithPropertiesAndAttributes
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
            mapper.Initialize(new[] { typeof(IInterfaceWithCustomAttributeThatHasNoDefaultConstructor) });
            var instance = mapper.CreateInstance(typeof(IInterfaceWithCustomAttributeThatHasNoDefaultConstructor));
            var attributes = instance.GetType().GetProperty("SomeProperty").GetCustomAttributes(typeof(NoDefaultConstructorAttribute), true);
            var attr = (NoDefaultConstructorAttribute)attributes[0];
            Assert.AreEqual(attr.Name, "Blah");
        }

        public interface IInterfaceWithCustomAttributeThatHasNoDefaultConstructor
        {
            [NoDefaultConstructor("Blah")]
            string SomeProperty { get; set; }
        }

        public class NoDefaultConstructorAttribute : Attribute
        {
            public string Name { get; set; }
            public NoDefaultConstructorAttribute(string name)
            {
                Name = name;
            }
        }

        [Test]
        public void Accept_Attributes_with_no_default_ctor_while_ctor_parameters_are_different_than_properties_of_custom_attribute()
        {
            var mapper = new MessageMapper();
            mapper.Initialize(new[] { typeof(IInterfaceWithCustomAttributeThatHasNoDefaultConstructorAndNoMatchingParameters) });
            var instance = mapper.CreateInstance(typeof(IInterfaceWithCustomAttributeThatHasNoDefaultConstructorAndNoMatchingParameters));
            var attributes = instance.GetType().GetProperty("SomeProperty").GetCustomAttributes(typeof(NoDefaultConstructorAndNoMatchingParametersAttribute), true);
            var attr = (NoDefaultConstructorAndNoMatchingParametersAttribute)attributes[0];
            Assert.AreEqual(attr.Name, "Blah");
        }
        public interface IInterfaceWithCustomAttributeThatHasNoDefaultConstructorAndNoMatchingParameters
        {
            [NoDefaultConstructorAndNoMatchingParameters("Blah", "Second Blah")]
            string SomeProperty { get; set; }
        }


        /// <summary>
        /// Break the heuristics of finding the properties from constructor parameter names
        /// </summary>
        public class NoDefaultConstructorAndNoMatchingParametersAttribute : Attribute
        {
            public string Name { get; set; }

            public NoDefaultConstructorAndNoMatchingParametersAttribute(string someParam, string someOtherParam)
            {
                Name = someParam;
            }
        }

        [Test]
        public void Generated_type_should_preserve_namespace_to_make_it_easier_for_users_to_define_custom_conventions()
        {
            var mapper = new MessageMapper();
            mapper.Initialize(new[] { typeof(IInterfaceToGenerate) });

            Assert.AreEqual(typeof(IInterfaceToGenerate).Namespace, mapper.CreateInstance(typeof(IInterfaceToGenerate)).GetType().Namespace);
        }

        public interface IInterfaceToGenerate : IMessage
        {
            string SomeProperty { get; set; }
        }

        [Test]
        public void Accept_attributes_with_value_attribute()
        {
            var mapper = new MessageMapper();
            mapper.Initialize(new[] { typeof(IMyEventWithAttributeWithBoolProperty) });
            var instance = mapper.CreateInstance(typeof(IMyEventWithAttributeWithBoolProperty));
            var attributes = instance.GetType().GetProperty("EventId").GetCustomAttributes(typeof(ValuePropertiesAttribute), true);
            var attr = attributes[0] as ValuePropertiesAttribute;
            Assert.AreEqual(attr != null && attr.FlagIsSet, true);
            if (attr != null)
            {
                Assert.AreEqual(attr.MyAge, 21);
            }
        }

        public interface IMyEventWithAttributeWithBoolProperty
        {
            [ValueProperties("bla bla", true, 21)]
            Guid EventId { get; set; }
        }

        public class ValuePropertiesAttribute : Attribute
        {
            public ValuePropertiesAttribute() { }

            public ValuePropertiesAttribute(string name, bool flag, int age)
            {
                Name = name;
                FlagIsSet = flag;
                MyAge = age;
            }

            public string Name { get; set; }

            public bool FlagIsSet { get; set; }

            public int MyAge { get; set; }
        }

        bool PropertyContainsAttribute(string propertyName, Type attributeType, object obj)
        {
            return obj.GetType().GetProperty(propertyName).GetCustomAttributes(attributeType, true).Length > 0;
        }
    }
}
