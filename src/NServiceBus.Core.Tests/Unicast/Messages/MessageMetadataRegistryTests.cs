namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using Unicast.Messages;

    [TestFixture]
    public class MessageMetadataRegistryTests
    {
        [Test]
        public void Should_throw_an_exception_for_a_unmapped_type()
        {
            var defaultMessageRegistry = new MessageMetadataRegistry(_ => false);
            Assert.Throws<Exception>(() => defaultMessageRegistry.GetMessageMetadata(typeof(int)));
        }

        [Test]
        public void Should_return_null_when_resolving_unknown_type_from_type_identifier()
        {
            var registry = new MessageMetadataRegistry(t => true);

            var metadata = registry.GetMessageMetadata("SomeNamespace.SomeType, SomeAssemblyName, Version=81.0.0.0, Culture=neutral, PublicKeyToken=null");

            Assert.IsNull(metadata);
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
            Assert.AreEqual(typeof(IInterfaceParent1), messageMetadata.MessageHierarchy.ToList()[1]);
            Assert.AreEqual(typeof(ConcreteParent1), messageMetadata.MessageHierarchy.ToList()[2]);
            Assert.AreEqual(typeof(IInterfaceParent1Base), messageMetadata.MessageHierarchy.ToList()[3]);
            Assert.AreEqual(typeof(ConcreteParentBase), messageMetadata.MessageHierarchy.ToList()[4]);
        }

        [TestCase("NServiceBus.Unicast.Tests.MessageMetadataRegistryTests+MyEvent, NonExistingAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b50674d1e0c6ce54")]
        [TestCase("NServiceBus.Unicast.Tests.MessageMetadataRegistryTests+MyEvent, NonExistingAssembly, Version=1.0.0.0, Culture=neutral")]
        [TestCase("NServiceBus.Unicast.Tests.MessageMetadataRegistryTests+MyEvent, NonExistingAssembly")]
        [TestCase("NServiceBus.Unicast.Tests.MessageMetadataRegistryTests+MyEvent")]
        public void Should_match_types_from_a_different_assembly(string typeName)
        {
            var defaultMessageRegistry = new MessageMetadataRegistry(new Conventions().IsMessageType);
            defaultMessageRegistry.RegisterMessageTypesFoundIn(new List<Type> { typeof(MyEvent) });

            var messageMetadata = defaultMessageRegistry.GetMessageMetadata(typeName);

            Assert.AreEqual(typeof(MyEvent), messageMetadata.MessageHierarchy.ToList()[0]);
        }

        [Test]
        public void Should_not_match_same_type_names_with_different_namespace()
        {
            var defaultMessageRegistry = new MessageMetadataRegistry(new Conventions().IsMessageType);
            defaultMessageRegistry.RegisterMessageTypesFoundIn(new List<Type> { typeof(MyEvent) });

            string typeIdentifier = typeof(MyEvent).AssemblyQualifiedName.Replace(typeof(MyEvent).FullName, $"SomeNamespace.{nameof(MyEvent)}");
            var messageMetadata = defaultMessageRegistry.GetMessageMetadata(typeIdentifier);

            Assert.IsNull(messageMetadata);
        }

        [Test]
        public void Should_resolve_uninitialized_types_from_loaded_assemblies()
        {
            var registry = new MessageMetadataRegistry(t => true);

            var metadata = registry.GetMessageMetadata(typeof(EndpointConfiguration).AssemblyQualifiedName);

            Assert.AreEqual(typeof(EndpointConfiguration), metadata.MessageType);
        }

        class MyEvent : ConcreteParent1, IInterfaceParent1
        {

        }

        class ConcreteParent1 : ConcreteParentBase
        {

        }
        class ConcreteParentBase : IMessage
        {

        }

        interface IInterfaceParent1 : IInterfaceParent1Base
        {

        }

        interface IInterfaceParent1Base : IMessage
        {

        }
    }
}