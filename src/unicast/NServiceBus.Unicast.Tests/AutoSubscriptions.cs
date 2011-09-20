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

    }

    public class CommandMessageHandler : IHandleMessages<CommandMessage>
    {
        public void Handle(CommandMessage message)
        {
            throw new System.NotImplementedException();
        }
    }
}