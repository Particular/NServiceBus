namespace MessageMapperTests
{
    using NServiceBus;
    using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
    using NUnit.Framework;

    [TestFixture]
    public class When_mapping_usinggenerics
    {

        [Test]
        public void Class_abstract_generic_with_only_properties_generic_should_not_be_mapped()
        {
            var mapper = new MessageMapper();
            var genericClassType = typeof(GenericAbstractCommand<>);
            mapper.Initialize(new[] { genericClassType });
            Assert.Null(mapper.GetMappedTypeFor(genericClassType));
        }
        public abstract class GenericAbstractCommand<T> : IMessage where T : Data
        {
            public T DataToTransfer { get; set; }
        }

        [Test]
        public void Class_generic_with_only_properties_generic_should_not_be_mapped()
        {
            var mapper = new MessageMapper();
            var abstractClassType = typeof(GenericCommand<>);
            mapper.Initialize(new[] { abstractClassType });
            Assert.Null(mapper.GetMappedTypeFor(abstractClassType));
        }

        public class GenericCommand<T> : IMessage where T : Data
        {
            public T DataToTransfer { get; set; }
        }


        [Test]
        public void Class_derived_from_generic_with_only_properties_generic_should_be_mapped()
        {
            var mapper = new MessageMapper();
            var abstractClassType = typeof(DerivedGenericCommand);
            mapper.Initialize(new[] { abstractClassType });
            Assert.NotNull(mapper.GetMappedTypeFor(abstractClassType));
        }
        public abstract class GenericBaseCommand<T> : IMessage where T : Data
        {
            public T DataToTransfer { get; set; }
        }
        public class DerivedGenericCommand : GenericBaseCommand<Data>
        {

        }

        [Test]
        public void Class_concrete_generic_with_only_properties_generic_should_be_mapped()
        {
            var mapper = new MessageMapper();
            var abstractClassType = typeof(GenericCommand<Data>);
            mapper.Initialize(new[] { abstractClassType });
            Assert.NotNull(mapper.GetMappedTypeFor(abstractClassType));
        }

        [Test]
        public void Class_abstract_with_only_properties_should_be_mapped()
        {
            var mapper = new MessageMapper();
            var abstractClassType = typeof(SimpleAbstractClass);
            mapper.Initialize(new[] { abstractClassType });
            Assert.NotNull(mapper.GetMappedTypeFor(abstractClassType));
        }
        public abstract class SimpleAbstractClass : IMessage
        {
            string SomeProperty { get; set; }
        }

        [Test,Ignore]
        public void Class_abstract_with_methods_should_not_be_mapped()
        {
            var mapper = new MessageMapper();
            var abstractClassType = typeof(SimpleAbstractClassWithMethods);
            mapper.Initialize(new[] { abstractClassType });
            Assert.Null(mapper.GetMappedTypeFor(abstractClassType));
        }
        public abstract class SimpleAbstractClassWithMethods : IMessage
        {
            string SomeProperty { get; set; }
            protected abstract void DoTest();
        }

        [Test]
        public void Interfaces_generic_with_only_properties_should_not_be_mapped()
        {
            var mapper = new MessageMapper();
            var genericInterfaceType = typeof(InterfaceGenericWithProperties<>);
            mapper.Initialize(new[] { genericInterfaceType });
            Assert.Null(mapper.GetMappedTypeFor(genericInterfaceType));
        }

        public interface InterfaceGenericWithProperties<T> : IMessage where T : Data
        {
            string SomeProperty { get; set; }
            T MessageProperty { get; set; }
        }

        [Test]
        public void Interfaces_generic_with_methods_should_not_be_mapped()
        {
            var mapper = new MessageMapper();
            var genericInterfaceType = typeof(InterfaceGenericWithMethods<>);
            mapper.Initialize(new[] { genericInterfaceType });
            Assert.Null(mapper.GetMappedTypeFor(genericInterfaceType));
        }
        public interface InterfaceGenericWithMethods<in T> : IMessage where T : Data
        {
            string SomeProperty { get; set; }
            void MethodOnInterface(T myMessage);
        }

        [Test]
        public void Interfaces_with_only_properties_should_be_mapped()
        {
            var mapper = new MessageMapper();
            var simpleInterfaceType = typeof(InterfaceWithOnlyProperties);
            mapper.Initialize(new[] { simpleInterfaceType });
            Assert.NotNull(mapper.GetMappedTypeFor(simpleInterfaceType));
        }

        public interface InterfaceWithOnlyProperties : IMessage
        {
            string SomeProperty { get; set; }
        }

        [Test]
        public void Interfaces_with_inheritance_and_property_overload_should_be_mapped()
        {
            var mapper = new MessageMapper();
            var genericInterfaceType = typeof(InterfaceWithGenericProperty<ISpecific>);
            mapper.Initialize(new[] { genericInterfaceType });
            Assert.NotNull(mapper.GetMappedTypeFor(genericInterfaceType));
        }

        public interface InterfaceWithGenericProperty
        {
            object Original { get; set; }
        }

        public interface InterfaceWithGenericProperty<T> : InterfaceWithGenericProperty
        {
            new T Original { get; set; }
        }

        public interface ISpecific
        {
            string Value { get; set; }
        }
    }

    public abstract class Data
    {
        
    }





}
