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
        [TestCase(typeof(SimpleConsumeEvent))]
        public void Simple_handler_should_be_classified_as_a_handler(Type handlerType)
        {
            Assert.IsTrue(RegisterHandlersInOrder.IsMessageHandler(handlerType));
        }

        [Test]
        [TestCase(typeof(ConcreteImplementationOfAbstractOldStyleHandler))]
        [TestCase(typeof(ConcreteImplementationOfAbstractNewStyleHandler))]
        [TestCase(typeof(ConcreteImplementationOfAbstractConsumeEvent))]
        public void Concrete_implementation_of_abstract_handler_should_be_classified_as_a_handler(Type handlerType)
        {
            Assert.IsTrue(RegisterHandlersInOrder.IsMessageHandler(handlerType));
        }

        [Test]
        [TestCase(typeof(AbstractOldStyleHandler))]
        [TestCase(typeof(AbstractNewStyleHandler))]
        [TestCase(typeof(AbstractConsumeEvent))]
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

        class SimpleNewStyleHandler : IConsumeMessage<SimpleMessage>
        {
            public void Handle(SimpleMessage message, IConsumeMessageContext messageContext)
            {
            }
        }

        class SimpleConsumeEvent : IConsumeEvent<SimpleMessage>
        {
            public void Handle(SimpleMessage message, IConsumeEventContext context)
            {
            }
        }

        class GenericTypeDefinitionOldStyleHandler<T> : IHandleMessages<SimpleMessage>
        {
            public void Handle(SimpleMessage message)
            {
            }
        }

        class GenericTypeDefinitionNewStyleHandler<T> : IConsumeMessage<SimpleMessage>
        {
            public void Handle(SimpleMessage message, IConsumeMessageContext messageContext)
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

        interface NewStyleInterfaceHandler : IConsumeMessage<SimpleMessage>
        {
        }

        interface InterfaceConsumeEvent : IConsumeEvent<SimpleMessage>
        {
        }

        class ConcreteImplementationOfAbstractOldStyleHandler : AbstractOldStyleHandler
        {
        }

        class ConcreteImplementationOfAbstractNewStyleHandler : AbstractNewStyleHandler
        {
        }

        class ConcreteImplementationOfAbstractConsumeEvent : AbstractConsumeEvent
        {
        }

        abstract class AbstractOldStyleHandler : IHandleMessages<SimpleMessage>
        {
            public void Handle(SimpleMessage message)
            {
            }
        }

        abstract class AbstractNewStyleHandler : IConsumeMessage<SimpleMessage>
        {
            public void Handle(SimpleMessage message, IConsumeMessageContext messageContext)
            {
            }
        }

        abstract class AbstractConsumeEvent : IConsumeEvent<SimpleMessage>
        {
            public void Handle(SimpleMessage message, IConsumeEventContext context)
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
