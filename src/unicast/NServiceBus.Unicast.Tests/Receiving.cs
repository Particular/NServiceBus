namespace NServiceBus.Unicast.Tests
{
    using Contexts;
    using NUnit.Framework;
   
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