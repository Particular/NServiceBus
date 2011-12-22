namespace NServiceBus.Unicast.Tests
{
    using System;
    using Contexts;
    using NUnit.Framework;
    using Rhino.Mocks;
    using Saga;
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
        public void Should_not_autosubscribe_messages_with_undefined_address()
        {

       
            RegisterMessageType<EventMessage>(Address.Undefined);
            RegisterMessageHandlerType<EventMessageHandler>();
            
            StartBus();

            messageSender.AssertWasNotCalled(x => x.Send(Arg<TransportMessage>.Is.Anything, Arg<Address>.Is.Equal(Address.Undefined)));
        }

    }

    [TestFixture]
    public class When_starting_an_endpoint_containing_a_saga : using_a_configured_unicastbus
    {
        [Test]
        public void Should_autosubscribe_the_saga_messagehandler()
        {

            var eventEndpointAddress = new Address("PublisherAddress", "localhost");

            RegisterMessageType<EventMessage>(eventEndpointAddress);
            RegisterMessageHandlerType<MySaga>();

            StartBus();

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Is.Anything, Arg<Address>.Is.Equal(eventEndpointAddress)));
        }
    }


    [TestFixture]
    public class When_starting_an_endpoint_containing_a_saga_and_autosubscription_of_sagas_is_off : using_a_configured_unicastbus
    {
        [Test]
        public void Should_not_autosubscribe_the_saga_messagehandler()
        {
            unicastBus.DoNotAutoSubscribeSagas = true;
      
            var eventEndpointAddress = new Address("PublisherAddress", "localhost");

            RegisterMessageType<EventMessage>(eventEndpointAddress);
            RegisterMessageHandlerType<MySaga>();
            StartBus();

            messageSender.AssertWasNotCalled(x => x.Send(Arg<TransportMessage>.Is.Anything, Arg<Address>.Is.Equal(eventEndpointAddress)));
        }
    }

    public class MySaga:Saga<MySagaData>,IAmStartedByMessages<EventMessage>
    {
        public void Handle(EventMessage message)
        {
            throw new NotImplementedException();
        }
    }

    public class MySagaData:ISagaEntity
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
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