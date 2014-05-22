namespace NServiceBus.Unicast.Tests
{
    using System;
    using Contexts;
    using NUnit.Framework;
    using Rhino.Mocks;

    [TestFixture]
    class When_publishing_a_command : using_the_unicastBus
    {
        [Test]
        public void Should_get_an_error_message()
        {
            RegisterMessageType<CommandMessage>();

            Assert.Throws<InvalidOperationException>(() => bus.Publish(new CommandMessage()));
        }
    }

    [TestFixture]
    class When_publishing_an_event : using_the_unicastBus
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

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Is.Anything, Arg<SendOptions>.Matches(o=>o.Destination == subscriber1)));

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Is.Anything, Arg<SendOptions>.Matches(o => o.Destination == subscriber2)));
        }
    }
}
