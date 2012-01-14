namespace NServiceBus.Unicast.Tests
{
    using Contexts;
    using NUnit.Framework;
    using Rhino.Mocks;
    using Transport;

    [TestFixture]
    public class When_receiving_a_regular_message : using_the_unicastbus
    {
        [Test]
        public void Should_invoke_the_registered_message_handlers()
        {
            var receivedMessage = Helpers.Helpers.Serialize(new EventMessage());
            
            RegisterMessageType<EventMessage>();
            RegisterMessageHandlerType<Handler1>();
            RegisterMessageHandlerType<Handler2>();

            ReceiveMessage(receivedMessage);


            Assert.True(Handler1.Called);
            Assert.True(Handler2.Called);
        }

     
    }


    [TestFixture]
    public class When_sending_messages_from_a_messagehandler : using_the_unicastbus
    {
        [Test]
        public void Should_set_the_related_to_header_with_the_id_of_the_current_message()
        {
            var receivedMessage = Helpers.Helpers.Serialize(new EventMessage());

            RegisterMessageType<EventMessage>();
            RegisterMessageType<CommandMessage>();
            RegisterMessageHandlerType<HandlerThatSendsAMessage>();

            ReceiveMessage(receivedMessage);


            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Headers[Monitoring.Headers.RelatedTo] == receivedMessage.IdForCorrelation), Arg<Address>.Is.Anything));
        }
    }


    [TestFixture]
    public class When_replying_with_a_command : using_the_unicastbus
    {
        [Test]
        public void Should_not_be_allowed()
        {
            var receivedMessage = Helpers.Helpers.Serialize(new EventMessage());

            RegisterMessageType<EventMessage>();
            RegisterMessageType<CommandMessage>();
            RegisterMessageHandlerType<HandlerThatRepliesWithACommand>();

            ReceiveMessage(receivedMessage);


            messageSender.AssertWasNotCalled(x => x.Send(Arg<TransportMessage>.Is.Anything, Arg<Address>.Is.Anything));
        }
    }


    class HandlerThatRepliesWithACommand : IHandleMessages<EventMessage>
    {
        public IBus Bus { get; set; }

        public void Handle(EventMessage message)
        {
            Bus.Reply(new CommandMessage());
        }
    }
    
    class HandlerThatSendsAMessage : IHandleMessages<EventMessage>
    {
        public IBus Bus { get; set; }

        public void Handle(EventMessage message)
        {
            Bus.Send(new CommandMessage());
        }
    }
    class Handler1:IHandleMessages<EventMessage>
    {
        public static bool Called;

        public void Handle(EventMessage message)
        {
            Called = true;
        }
    }

    class Handler2 : IHandleMessages<EventMessage>
    {
        public static bool Called;

        public void Handle(EventMessage message)
        {
            Called = true;
        }
    }

}