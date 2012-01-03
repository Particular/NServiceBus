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
        public void Should_set_the_correlation_id_to_the_current_message()
        {
            var receivedMessage = Helpers.Helpers.Serialize(new EventMessage());

            RegisterMessageType<EventMessage>();
            RegisterMessageType<CommandMessage>();
            RegisterMessageHandlerType<HandlerThatSendsAMessage>();

            ReceiveMessage(receivedMessage);


            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.CorrelationId == receivedMessage.IdForCorrelation), Arg<Address>.Is.Anything));
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