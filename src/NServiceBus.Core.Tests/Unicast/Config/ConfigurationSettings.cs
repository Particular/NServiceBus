namespace NServiceBus.Unicast.Config.Tests
{
    using System;
    using NServiceBus.Features;
    using NUnit.Framework;

    [TestFixture]
    public class ConfigUnicastBusTests
    {
        [Test]
        [TestCase(typeof(SimpleOldStyleHandler))]
        [TestCase(typeof(SimpleNewStyleHandler))]
        [TestCase(typeof(SimpleSubscribe))]
        public void Simple_handler_should_be_classified_as_a_handler(Type handlerType)
        {
            Assert.IsTrue(RegisterHandlersInOrder.IsMessageHandler(handlerType));
        }

        [Test]
        [TestCase(typeof(ConcreteImplementationOfAbstractOldStyleHandler))]
        [TestCase(typeof(ConcreteImplementationOfAbstractNewStyleHandler))]
        [TestCase(typeof(ConcreteImplementationOfAbstractSubscribe))]
        public void Concrete_implementation_of_abstract_handler_should_be_classified_as_a_handler(Type handlerType)
        {
            Assert.IsTrue(RegisterHandlersInOrder.IsMessageHandler(handlerType));
        }

        [Test]
        [TestCase(typeof(AbstractOldStyleHandler))]
        [TestCase(typeof(AbstractNewStyleHandler))]
        [TestCase(typeof(AbstractSubscribe))]
        public void Abstract_handler_should_not_be_classified_as_a_handler(Type handlerType)
        {
            Assert.IsFalse(RegisterHandlersInOrder.IsMessageHandler(handlerType));
        }

        [Test]
        public void Not_implementing_IHandleMessages_should_not_be_classified_as_a_handler()
        {
            Assert.IsFalse(RegisterHandlersInOrder.IsMessageHandler(typeof(NotImplementingIHandleMessages)));
        }

        [Test]
        [TestCase(typeof(OldStyleInterfaceHandler))]
        [TestCase(typeof(NewStyleInterfaceHandler))]
        public void Interface_handler_should_not_be_classified_as_a_handler(Type handlerType)
        {
            Assert.IsFalse(RegisterHandlersInOrder.IsMessageHandler(handlerType));
        }

        [Test]
        [TestCase(typeof(GenericTypeDefinitionOldStyleHandler<>))]
        [TestCase(typeof(GenericTypeDefinitionNewStyleHandler<>))]
        public void Generic_type_definition_handler_should_not_be_classified_as_a_handler(Type handlerType)
        {
            Assert.IsFalse(RegisterHandlersInOrder.IsMessageHandler(handlerType));
        }

        [Test]
        [TestCase(typeof(GenericTypeDefinitionOldStyleHandler<string>))]
        [TestCase(typeof(GenericTypeDefinitionNewStyleHandler<string>))]
        public void Specific_generic_type_definition_handler_should_not_be_classified_as_a_handler(Type handlerType)
        {
            Assert.IsTrue(RegisterHandlersInOrder.IsMessageHandler(handlerType));
        }

        [Test]
        [TestCase(typeof(GenericImplementedOldStyleHandler))]
        [TestCase(typeof(GenericImplementedNewStyleHandler))]
        public void Generic_implemented_type_definition_handler_should_not_be_classified_as_a_handler(Type handlerType)
        {
            Assert.IsTrue(RegisterHandlersInOrder.IsMessageHandler(handlerType));
        }

        class SimpleOldStyleHandler : IHandleMessages<SimpleMessage>
        {
            public void Handle(SimpleMessage message)
            {
            }
        }

        class SimpleNewStyleHandler : IHandle<SimpleMessage>
        {
            public void Handle(SimpleMessage message, IHandleContext context)
            {
            }
        }

        class SimpleSubscribe : ISubscribe<SimpleMessage>
        {
            public void Handle(SimpleMessage message, ISubscribeContext context)
            {
            }
        }

        class GenericTypeDefinitionOldStyleHandler<T> : IHandleMessages<SimpleMessage>
        {
            public void Handle(SimpleMessage message)
            {
            }
        }

        class GenericTypeDefinitionNewStyleHandler<T> : IHandle<SimpleMessage>
        {
            public void Handle(SimpleMessage message, IHandleContext context)
            {
            }
        }

        class GenericImplementedOldStyleHandler : GenericTypeDefinitionOldStyleHandler<string>
        {
        }

        class GenericImplementedNewStyleHandler : GenericTypeDefinitionNewStyleHandler<string>
        {
        }

        interface OldStyleInterfaceHandler : IHandleMessages<SimpleMessage>
        {
        }

        interface NewStyleInterfaceHandler : IHandle<SimpleMessage>
        {
        }

        interface InterfaceSubscribe : ISubscribe<SimpleMessage>
        {
        }

        class ConcreteImplementationOfAbstractOldStyleHandler : AbstractOldStyleHandler
        {
        }

        class ConcreteImplementationOfAbstractNewStyleHandler : AbstractNewStyleHandler
        {
        }

        class ConcreteImplementationOfAbstractSubscribe : AbstractSubscribe
        {
        }

        abstract class AbstractOldStyleHandler : IHandleMessages<SimpleMessage>
        {
            public void Handle(SimpleMessage message)
            {
            }
        }

        abstract class AbstractNewStyleHandler : IHandle<SimpleMessage>
        {
            public void Handle(SimpleMessage message, IHandleContext context)
            {
            }
        }

        abstract class AbstractSubscribe : ISubscribe<SimpleMessage>
        {
            public void Handle(SimpleMessage message, ISubscribeContext context)
            {
            }
        }

        abstract class NotImplementingIHandleMessages
        {
        }

        class SimpleMessage
        {
        }
    }

}
