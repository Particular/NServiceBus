namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using Unicast.Messages;

    [TestFixture]
    public class DefaultMessageRegistryTests
    {
        [TestFixture]
        public class When_getting_message_definition
        {
            [Test]
            public void Should_throw_an_exception_for_a_unmapped_type()
            {
                var defaultMessageRegistry = new MessageMetadataRegistry(_ => false);
                Assert.Throws<Exception>(() => defaultMessageRegistry.GetMessageMetadata(typeof(int)));
            }

            [Test]
            public void Should_return_metadata_for_a_mapped_type()
            {
                var defaultMessageRegistry = new MessageMetadataRegistry(type => type == typeof(int));
                defaultMessageRegistry.RegisterMessageTypesFoundIn(new List<Type> { typeof(int) });

                var messageMetadata = defaultMessageRegistry.GetMessageMetadata(typeof(int));

                Assert.AreEqual(typeof(int), messageMetadata.MessageType);
                Assert.AreEqual(1, messageMetadata.MessageHierarchy.Count());
            }


            [Test]
            public void Should_return_the_correct_parent_hierarchy()
            {
                var defaultMessageRegistry = new MessageMetadataRegistry(new Conventions().IsMessageType);

                defaultMessageRegistry.RegisterMessageTypesFoundIn(new List<Type> { typeof(MyEvent) });
                var messageMetadata = defaultMessageRegistry.GetMessageMetadata(typeof(MyEvent));

                Assert.AreEqual(5, messageMetadata.MessageHierarchy.Count());

                Assert.AreEqual(typeof(MyEvent), messageMetadata.MessageHierarchy.ToList()[0]);
                Assert.AreEqual(typeof(InterfaceParent1), messageMetadata.MessageHierarchy.ToList()[1]);
                Assert.AreEqual(typeof(ConcreteParent1), messageMetadata.MessageHierarchy.ToList()[2]);
                Assert.AreEqual(typeof(InterfaceParent1Base), messageMetadata.MessageHierarchy.ToList()[3]);
                Assert.AreEqual(typeof(ConcreteParentBase), messageMetadata.MessageHierarchy.ToList()[4]);
            }

            [TestCase("NServiceBus.Unicast.Tests.DefaultMessageRegistryTests+When_getting_message_definition+MyEvent, NonExistingAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b50674d1e0c6ce54")]
            [TestCase("NServiceBus.Unicast.Tests.DefaultMessageRegistryTests+When_getting_message_definition+MyEvent, NonExistingAssembly, Version=1.0.0.0, Culture=neutral")]
            [TestCase("NServiceBus.Unicast.Tests.DefaultMessageRegistryTests+When_getting_message_definition+MyEvent, NonExistingAssembly")]
            [TestCase("NServiceBus.Unicast.Tests.DefaultMessageRegistryTests+When_getting_message_definition+MyEvent")]
            public void Should_match_types_from_a_different_assembly(string typeName)
            {
                var defaultMessageRegistry = new MessageMetadataRegistry(new Conventions().IsMessageType);
                defaultMessageRegistry.RegisterMessageTypesFoundIn(new List<Type> { typeof(MyEvent) });

                var messageMetadata = defaultMessageRegistry.GetMessageMetadata(typeName);

                Assert.AreEqual(typeof(MyEvent), messageMetadata.MessageHierarchy.ToList()[0]);
            }

            class MyEvent : ConcreteParent1, InterfaceParent1
            {

            }

            class ConcreteParent1 : ConcreteParentBase
            {

            }
            class ConcreteParentBase : IMessage
            {

            }

            interface InterfaceParent1 : InterfaceParent1Base
            {

            }

            interface InterfaceParent1Base : IMessage
            {

            }

        }
    }
}