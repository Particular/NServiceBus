namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Threading;
    using Contexts;
    using NUnit.Framework;
    using Rhino.Mocks;
    using Saga;

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
        public void Should_not_autosubscribe_messages_by_default()
        {

            var endpointAddress = new Address("MyEndpoint", "localhost");

            RegisterMessageType<MyMessage>(endpointAddress);
            RegisterMessageHandlerType<MyMessageHandler>();

            StartBus();

            messageSender.AssertWasNotCalled(x => x.Send(Arg<TransportMessage>.Is.Anything, Arg<Address>.Is.Equal(endpointAddress)));
        }

        [Test]
        public void Should_not_autosubscribe_messages_unless_asked_to_by_the_users()
        {

            var endpointAddress = new Address("MyEndpoint", "localhost");
            autoSubscriptionStrategy.SubscribePlainMessages = true;

            RegisterMessageType<MyMessage>(endpointAddress);
            RegisterMessageHandlerType<MyMessageHandler>();

            StartBus();

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Is.Anything, Arg<Address>.Is.Equal(endpointAddress)));
        }


        [Test]
        public void Should_not_autosubscribe_messages_with_undefined_address()
        {

       
            RegisterMessageType<EventMessage>(Address.Undefined);
            RegisterMessageHandlerType<EventMessageHandler>();
            
            StartBus();

            messageSender.AssertWasNotCalled(x => x.Send(Arg<TransportMessage>.Is.Anything, Arg<Address>.Is.Equal(Address.Undefined)));
        }

        class MyMessage:IMessage
        {
            
        }

        class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public void Handle(MyMessage message)
            {
                throw new System.NotImplementedException();
            }
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
            Thread.Sleep(5000); //Wait for subscriptions to happen

            AssertSubscription(m => true,
                              eventEndpointAddress);
        }
    }


    [TestFixture]
    public class When_starting_an_endpoint_containing_a_saga_and_autosubscription_of_sagas_is_off : using_a_configured_unicastbus
    {
        [Test]
        public void Should_not_autosubscribe_the_saga_messagehandler()
        {
            autoSubscriptionStrategy.DoNotAutoSubscribeSagas = true;
      
            var eventEndpointAddress = new Address("PublisherAddress", "localhost");

            RegisterMessageType<EventMessage>(eventEndpointAddress);
            RegisterMessageHandlerType<MySaga>();
            StartBus();
            autoSubscriptionStrategy.DoNotAutoSubscribeSagas = false;
      
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


    [TestFixture]
    public class When_autosubscribing_a_saga_that_handles_a_superclass_event : using_a_configured_unicastbus
    {
        protected override System.Collections.Generic.IEnumerable<Type> KnownMessageTypes()
        {
            return new[] {typeof (EventWithParent), typeof (EventMessageBase)};
        }
        [Test]
        public void Should_autosubscribe_the_saga_messagehandler()
        {

            var eventEndpointAddress = new Address("PublisherAddress", "localhost");

            RegisterMessageType<EventWithParent>(eventEndpointAddress);
            RegisterMessageHandlerType<MySagaThatReactsOnASuperClassEvent>();

            StartBus();

            AssertSubscription(m => true,
                              eventEndpointAddress);
        }
    }
    public class MySagaThatReactsOnASuperClassEvent : Saga<MySagaData>, IAmStartedByMessages<EventMessageBase>
    {
        public void Handle(EventMessageBase message)
        {
            throw new NotImplementedException();
        }
    }
    public class EventMessageBase:IEvent
    {
        
    }

    public class EventWithParent : EventMessageBase
    {

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