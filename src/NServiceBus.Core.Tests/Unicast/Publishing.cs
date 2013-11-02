namespace NServiceBus.Unicast.Tests
{
    using System;
    using Contexts;
    using NUnit.Framework;
    using Rhino.Mocks;

    [TestFixture]
    public class When_publishing_a_command_messages : using_the_unicastBus
    {
        [Test]
        public void Should_get_an_error_messages()
        {
            RegisterMessageType<CommandMessage>();

            Assert.Throws<InvalidOperationException>(() => bus.Publish(new CommandMessage()));
        }
    }

    [TestFixture]
    public class When_publishing_a_event_messages : using_the_unicastBus
    {
        [Test]
        public void Should_send_a_message_to_each_subscriber()
        {
            var subscriber1 = new Address("sub1", ".");
            var subscriber2 = new Address("sub2", ".");

            RegisterMessageType<EventMessage>();
            subscriptionStorage.FakeSubscribe<EventMessage>(subscriber1);
            subscriptionStorage.FakeSubscribe<EventMessage>(subscriber2);

            bus.Publish(new EventMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Is.Anything, Arg<Address>.Is.Equal(subscriber1)));

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Is.Anything, Arg<Address>.Is.Equal(subscriber2)));
        }

        [Test]
        public void Should_fire_the_no_subscribers_for_message_if_no_subscribers_exists()
        {
          
            RegisterMessageType<EventMessage>();

            var eventFired = false;
            var eventMessage = new EventMessage();

            unicastBus.NoSubscribersForMessage += (sender, args) =>
                {
                    eventFired = true;
                    Assert.AreSame(eventMessage,args.Message);
                };
            bus.Publish(eventMessage);

            Assert.True(eventFired);
         }
    }
}
