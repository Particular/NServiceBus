namespace NServiceBus.Unicast.Config.Tests
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class ConfigUnicastBusTests
    {

        [Test]
        public void Simple_handler_should_be_classified_as_a_handler()
        {
            Assert.IsTrue(ReceiveComponent.IsMessageHandler(typeof(SimpleHandler)));
        }

        [Test]
        public void Concrete_implementation_of_abstract_handler_should_be_classified_as_a_handler()
        {
            Assert.IsTrue(ReceiveComponent.IsMessageHandler(typeof(ConcreteImplementationOfAbstractHandler)));
        }

        [Test]
        public void Abstract_handler_should_not_be_classified_as_a_handler()
        {
            Assert.IsFalse(ReceiveComponent.IsMessageHandler(typeof(AbstractHandler)));
        }

        [Test]
        public void Not_implementing_IHandleMessages_should_not_be_classified_as_a_handler()
        {
            Assert.IsFalse(ReceiveComponent.IsMessageHandler(typeof(NotImplementingIHandleMessages)));
        }

        [Test]
        public void Interface_handler_should_not_be_classified_as_a_handler()
        {
            Assert.IsFalse(ReceiveComponent.IsMessageHandler(typeof(InterfaceHandler)));
        }

        [Test]
        public void Generic_type_definition_handler_should_not_be_classified_as_a_handler()
        {
            Assert.IsFalse(ReceiveComponent.IsMessageHandler(typeof(GenericTypeDefinitionHandler<>)));
        }

        [Test]
        public void Specific_generic_type_definition_handler_should_be_classified_as_a_handler()
        {
            Assert.IsTrue(ReceiveComponent.IsMessageHandler(typeof(GenericTypeDefinitionHandler<string>)));
        }

        [Test]
        public void Generic_implemented_type_definition_handler_should_be_classified_as_a_handler()
        {
            Assert.IsTrue(ReceiveComponent.IsMessageHandler(typeof(GenericImplementedHandler)));
        }


        public class SimpleHandler : IHandleMessages<SimpleMessage>
        {
            public Task Handle(SimpleMessage message, IMessageHandlerContext context)
            {
                return Task.CompletedTask;
            }
        }

        public class GenericTypeDefinitionHandler<T> : IHandleMessages<SimpleMessage>
        {
            public Task Handle(SimpleMessage message, IMessageHandlerContext context)
            {
                return Task.CompletedTask;
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
            public Task Handle(SimpleMessage message, IMessageHandlerContext context)
            {
                return Task.CompletedTask;
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
