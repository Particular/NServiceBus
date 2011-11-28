namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Contexts;
    using NUnit.Framework;
    using Rhino.Mocks;
    using Subscriptions;
    using Transport;

    [TestFixture]
    public class When_publishing_a_command_messages : using_the_unicastbus
    {
        [Test]
        public void Should_get_an_error_messages()
        {
            RegisterMessageType<CommandMessage>();

            Assert.Throws<InvalidOperationException>(() => bus.Publish(new CommandMessage()));
        }
    }


    [TestFixture]
    public class When_publishing_a_event_messages : using_the_unicastbus
    {
        [Test]
        public void Should_get_an_error_messages()
        {
            var subscriberAddress = new Address("sub1", ".");

            RegisterMessageType<EventMessage>();
            subscriptionStorage.FakeSubscribe<EventMessage>(subscriberAddress);

            bus.Publish(new EventMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Is.Anything, Arg<Address>.Is.Equal(subscriberAddress)));
        }
    }
}