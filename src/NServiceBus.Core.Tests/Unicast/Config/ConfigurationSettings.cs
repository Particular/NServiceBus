namespace NServiceBus.Unicast.Config.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class ConfigUnicastBusTests
    {

        [Test]
        public void Simple_handler_should_be_classified_as_a_handler()
        {
            Assert.IsTrue(ConfigUnicastBus.IsMessageHandler(typeof(SimpleHandler)));
        }

        [Test]
        public void Concrete_implementation_of_abstract_handler_should_be_classified_as_a_handler()
        {
            Assert.IsTrue(ConfigUnicastBus.IsMessageHandler(typeof(ConcreteImplementationOfAbstractHandler)));
        }

        [Test]
        public void Abstract_handler_should_not_be_classified_as_a_handler()
        {
            Assert.IsFalse(ConfigUnicastBus.IsMessageHandler(typeof(AbstractHandler)));
        }

        [Test]
        public void Not_implementing_IHandleMessages_should_not_be_classified_as_a_handler()
        {
            Assert.IsFalse(ConfigUnicastBus.IsMessageHandler(typeof(NotImplementingIHandleMessages)));
        }

        [Test]
        public void Interface_handler_should_not_be_classified_as_a_handler()
        {
            Assert.IsFalse(ConfigUnicastBus.IsMessageHandler(typeof(InterfaceHandler)));
        }

        [Test]
        public void Generic_type_definition_handler_should_not_be_classified_as_a_handler()
        {
            Assert.IsFalse(ConfigUnicastBus.IsMessageHandler(typeof(GenericTypeDefinitionHandler<>)));
        }

        [Test]
        public void Specific_generic_type_definition_handler_should_not_be_classified_as_a_handler()
        {
            Assert.IsFalse(ConfigUnicastBus.IsMessageHandler(typeof(GenericTypeDefinitionHandler<string>)));
        }

        [Test]
        public void Generic_implemented_type_definition_handler_should_not_be_classified_as_a_handler()
        {
            Assert.IsTrue(ConfigUnicastBus.IsMessageHandler(typeof(GenericImplementedHandler)));
        }

        
        public class SimpleHandler : IHandleMessages<SimpleMessage>
        {
            public void Handle(SimpleMessage message)
            {
            }
        }

        public class GenericTypeDefinitionHandler<T> : IHandleMessages<SimpleMessage>
        {
            public void Handle(SimpleMessage message)
            {
            }
        }

        public class GenericImplementedHandler : GenericTypeDefinitionHandler<string>
        {
        }

        public interface InterfaceHandler : IHandleMessages<SimpleMessage>
        {
        }

        public class ConcreteImplementationOfAbstractHandler : AbstractHandler
        {
        }

        public abstract class AbstractHandler : IHandleMessages<SimpleMessage>
        {
            public void Handle(SimpleMessage message)
            {
            }
        }

        public abstract class NotImplementingIHandleMessages
        {
        }

        public class SimpleMessage
        {
        }
    }

}
