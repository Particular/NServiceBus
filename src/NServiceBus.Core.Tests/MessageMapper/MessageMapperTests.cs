namespace MessageMapperTests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;
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

            var ex = Assert.Throws<Exception>(() => mapper.Initialize(new[] { typeof(InterfaceMessageWithIllegalInterfaceProperty) }));
            StringAssert.Contains($"Cannot generate a concrete implementation for '{nameof(IIllegalProperty)}' because it contains methods. Ensure that all interfaces used as messages do not contain methods.", ex.Message);
        }

        [Test]
        public void Should_fail_for_non_public_interface_message()
        {
            var mapper = new MessageMapper();

            var ex = Assert.Throws<Exception>(() => mapper.Initialize(new[] { typeof(IPrivateInterfaceMessage) }));
            StringAssert.Contains($"Cannot generate a concrete implementation for '{typeof(IPrivateInterfaceMessage).FullName}' because it is not public. Ensure that all interfaces used as messages are public.", ex.Message);
        }

        [Test]
        public void CreateInstance_should_initialize_interface_message_type_on_demand()
        {
            var mapper = new MessageMapper();

            var messageInstance = mapper.CreateInstance<ISampleMessageInterface>();

            Assert.IsNotNull(messageInstance);
            Assert.IsInstanceOf<ISampleMessageInterface>(messageInstance);
        }

        [Test]
        public void CreateInstance_should_initialize_message_type_on_demand()
        {
            var mapper = new MessageMapper();

            var messageInstance = mapper.CreateInstance<SampleMessageClass>();

            Assert.IsNotNull(messageInstance);
            Assert.AreEqual(typeof(SampleMessageClass), messageInstance.GetType());
            Assert.IsTrue(messageInstance.CtorInvoked);
        }

        [Test]
        // this is not desired behavior and just documents current behavior
        public void CreateInstance_should_not_initialize_message_type_implementing_IEnumerable()
        {
            var mapper = new MessageMapper();

            var messageInstance = mapper.CreateInstance<ClassImplementingIEnumerable<string>>();

            Assert.IsNotNull(messageInstance);
            Assert.IsFalse(messageInstance.CtorInvoked);
        }

        public class SampleMessageClass
        {
            public SampleMessageClass()
            {
                CtorInvoked = true;
            }

            public bool CtorInvoked { get; }
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

        public interface InterfaceMessageWithIllegalInterfaceProperty
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
    }
}