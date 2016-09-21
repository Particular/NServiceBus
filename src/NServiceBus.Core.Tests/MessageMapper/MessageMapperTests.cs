﻿namespace MessageMapperTests
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

            Assert.Throws<Exception>(() => mapper.Initialize(new[] { typeof(InterfaceMessageWithIllegalInterfaceProperty) }));
        }


        public class SampleMessageClass
        {
        }

        public interface ISampleMessageInterface
        {
        }

        public class ClassImplementingIEnumerable<TItem> : IEnumerable<TItem>
        {
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
    }
}