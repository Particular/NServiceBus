namespace NServiceBus.Unicast.Config.Tests;

using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public class ConfigUnicastBusTests
{

    [Test]
    public void Simple_handler_should_be_classified_as_a_handler()
    {
        Assert.That(MessageHandlerRegistry.IsMessageHandler(typeof(SimpleHandler)), Is.True);
    }

    [Test]
    public void Concrete_implementation_of_abstract_handler_should_be_classified_as_a_handler()
    {
        Assert.That(MessageHandlerRegistry.IsMessageHandler(typeof(ConcreteImplementationOfAbstractHandler)), Is.True);
    }

    [Test]
    public void Abstract_handler_should_not_be_classified_as_a_handler()
    {
        Assert.That(MessageHandlerRegistry.IsMessageHandler(typeof(AbstractHandler)), Is.False);
    }

    [Test]
    public void Not_implementing_IHandleMessages_should_not_be_classified_as_a_handler()
    {
        Assert.That(MessageHandlerRegistry.IsMessageHandler(typeof(NotImplementingIHandleMessages)), Is.False);
    }

    [Test]
    public void Interface_handler_should_not_be_classified_as_a_handler()
    {
        Assert.That(MessageHandlerRegistry.IsMessageHandler(typeof(IInterfaceHandler)), Is.False);
    }

    [Test]
    public void Generic_type_definition_handler_should_not_be_classified_as_a_handler()
    {
        Assert.That(MessageHandlerRegistry.IsMessageHandler(typeof(GenericTypeDefinitionHandler<>)), Is.False);
    }

    [Test]
    public void Specific_generic_type_definition_handler_should_be_classified_as_a_handler()
    {
        Assert.That(MessageHandlerRegistry.IsMessageHandler(typeof(GenericTypeDefinitionHandler<string>)), Is.True);
    }

    [Test]
    public void Generic_implemented_type_definition_handler_should_be_classified_as_a_handler()
    {
        Assert.That(MessageHandlerRegistry.IsMessageHandler(typeof(GenericImplementedHandler)), Is.True);
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

    public interface IInterfaceHandler : IHandleMessages<SimpleMessage>
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
