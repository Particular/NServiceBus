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
}