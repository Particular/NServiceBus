namespace NServiceBus.Unicast.Tests
{
    using NUnit.Framework;
    using Rhino.Mocks;
    using SomeUserNamespace;
    using Transport;

    [TestFixture]
    public class When_starting_an_endpoint_with_autosubscribe_turned_on : using_a_configured_unicastbus
    {
        [Test]
        public void Should_not_autosubscribe_commands()
        {

            var commandEndpointAddress = new Address("CommandEndpoint", "localhost");
            
            RegisterMessageType<CommandMessage>(commandEndpointAddress);
            RegisterMessageHandlerType<CommandMessageHandler>();
            
            StartBus();

            messageSender.AssertWasNotCalled(x => x.Send(Arg<TransportMessage>.Is.Anything, Arg<Address>.Is.Equal(commandEndpointAddress)));
        }

        [Test]
        public void Should_not_autosubscribe_messages_with_undefined_addre()
        {

       
            RegisterMessageType<EventMessage>(Address.Undefined);
            RegisterMessageHandlerType<EventMessageHandler>();
            
            StartBus();

            messageSender.AssertWasNotCalled(x => x.Send(Arg<TransportMessage>.Is.Anything, Arg<Address>.Is.Equal(Address.Undefined)));
        }

    }
    public class EventMessageHandler : IHandleMessages<EventMessage>
    {
        public void Handle(EventMessage message)
        {
            throw new System.NotImplementedException();
        }
    }
    public class CommandMessageHandler : IHandleMessages<CommandMessage>
    {
        public void Handle(CommandMessage message)
        {
            throw new System.NotImplementedException();
        }
    }
}