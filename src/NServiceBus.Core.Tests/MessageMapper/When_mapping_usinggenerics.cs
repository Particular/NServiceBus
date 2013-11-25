namespace NServiceBus.MessageInterfaces.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class When_mapping_usinggenerics
    {
        IMessageMapper mapper;

        [SetUp]
        public void SetUp()
        {
            mapper = new MessageMapper.Reflection.MessageMapper();
        }

        [Test]
        public void Class_abstract_generic_with_only_properties_generic_should_not_be_mapped()
        {
            var genericClassType = typeof(GenericAbstractCommand<>);
            mapper.Initialize(new[] { genericClassType });
            Assert.Null(mapper.GetMappedTypeFor(genericClassType));
        }

        [Test]
        public void Class_generic_with_only_properties_generic_should_not_be_mapped()
        {
            var abstractClassType = typeof(GenericCommand<>);
            mapper.Initialize(new[] { abstractClassType });
            Assert.Null(mapper.GetMappedTypeFor(abstractClassType));
        }

        [Test]
        public void Class_derived_from_generic_with_only_properties_generic_should_be_mapped()
        {
            var abstractClassType = typeof(DerivedGenericCommand);
            mapper.Initialize(new[] { abstractClassType });
            Assert.NotNull(mapper.GetMappedTypeFor(abstractClassType));
        }

        [Test]
        public void Class_concrete_generic_with_only_properties_generic_should_be_mapped()
        {
            var abstractClassType = typeof(GenericCommand<Data>);
            mapper.Initialize(new[] { abstractClassType });
            Assert.NotNull(mapper.GetMappedTypeFor(abstractClassType));
        }

        [Test]
        public void Class_abstract_with_only_properties_should_be_mapped()
        {
            var abstractClassType = typeof(SimpleAbstractClass);
            mapper.Initialize(new[] { abstractClassType });
            Assert.NotNull(mapper.GetMappedTypeFor(abstractClassType));
        }

        [Test,Ignore]
        public void Class_abstract_with_methods_should_not_be_mapped()
        {
            var abstractClassType = typeof(SimpleAbstractClassWithMethods);
            mapper.Initialize(new[] { abstractClassType });
            Assert.Null(mapper.GetMappedTypeFor(abstractClassType));
        }

        [Test]
        public void Interfaces_generic_with_only_properties_should_not_be_mapped()
        {
            var genericInterfaceType = typeof(InterfaceGenericWithProperties<>);
            mapper.Initialize(new[] { genericInterfaceType });
            Assert.Null(mapper.GetMappedTypeFor(genericInterfaceType));
        }

        [Test]
        public void Interfaces_generic_with_methods_should_not_be_mapped()
        {
            var genericInterfaceType = typeof(InterfaceGenericWithMethods<>);
            mapper.Initialize(new[] { genericInterfaceType });
            Assert.Null(mapper.GetMappedTypeFor(genericInterfaceType));
        }

        [Test]
        public void Interfaces_with_only_properties_should_be_mapped()
        {
            var simpleInterfaceType = typeof(InterfaceWithProperties);
            mapper.Initialize(new[] { simpleInterfaceType });
            Assert.NotNull(mapper.GetMappedTypeFor(simpleInterfaceType));
        }

    }

    public abstract class Data
    {
        
    }

    public interface InterfaceGenericWithProperties<T> : IMessage where T : Data
    {
        string SomeProperty { get; set; }
        T MessageProperty { get; set; }
    }

    public interface InterfaceGenericWithMethods<in T> : IMessage where T : Data
    {
        string SomeProperty { get; set; }
        void MethodOnInterface(T myMessage);
    }

    public abstract class GenericAbstractCommand<T> : IMessage where T : Data
    {
        public T DataToTransfer { get; set; }
    }

    public class GenericCommand<T> : IMessage where T : Data
    {
        public T DataToTransfer { get; set; }
    }

    public class DerivedGenericCommand : GenericAbstractCommand<Data>
    {
        
    }

    public interface InterfaceSimplWithProperties : IMessage
    {
        string SomeProperty { get; set; }
    }

    public abstract class SimpleAbstractClass : IMessage
    {
        string SomeProperty { get; set; }
    }

    public abstract class SimpleAbstractClassWithMethods : IMessage
    {
        string SomeProperty { get; set; }
        protected abstract void DoTest();
    }
}
