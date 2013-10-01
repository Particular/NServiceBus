namespace NServiceBus.Core.Tests
{
    using MessageInterfaces.MessageMapper.Reflection;
    using NUnit.Framework;

    [TestFixture]
    public class When_mapping_classes
    {

        [Test]
        public void Class_with_only_properties_should_be_mapped()
        {
            var mapper = new MessageMapper();
            mapper.Initialize(new[] {typeof(ClassWithProperties)});

            Assert.NotNull(mapper.GetMappedTypeFor(typeof(ClassWithProperties)));
            var result = mapper.CreateInstance<ClassWithProperties>(null);
            Assert.IsInstanceOf<ClassWithProperties>(result);
        }
        
        [Test]
        public void Class_with_methods_should_be_not_ignored()
        {
            var mapper = new MessageMapper();
            mapper.Initialize(new[] {typeof(ClassWithMethods)});

            Assert.AreEqual(typeof(ClassWithMethods), mapper.GetMappedTypeFor(typeof(ClassWithMethods)));
            var result = mapper.CreateInstance<ClassWithMethods>(null);
            Assert.IsInstanceOf<ClassWithMethods>(result);
        }


        public class ClassWithProperties : IMessage
        {
            string SomeProperty { get; set; }
        }

        public class ClassWithMethods
        {
            string SomeProperty { get; set; }
            void MethodOnInterface(){}
        }


    }
}