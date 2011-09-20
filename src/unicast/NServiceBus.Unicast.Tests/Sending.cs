namespace NServiceBus.Unicast.Tests
{
    using System;
    using NUnit.Framework;
    using Rhino.Mocks;
    using SomeUserNamespace;
    using Transport;

    [TestFixture]
    public class When_sending_a_event_message : using_the_unicastbus
    {
        [Test]
        public void Should_get_an_error_messages()
        {
            RegisterMessageType<EventMessage>();
            Assert.Throws<InvalidOperationException>(() => bus.Send(new EventMessage()));
        }
    }
    [TestFixture]
    public class When_sending_a_event_message_to_sites : using_the_unicastbus
    {
        [Test]
        public void Should_get_an_error_messages()
        {
            RegisterMessageType<EventMessage>();
            Assert.Throws<InvalidOperationException>(() => bus.SendToSites(new []{"KeyA"}, new EventMessage()));
        }
    }

    [TestFixture]
    public class When_sending_messages_to_sites : using_the_unicastbus
    {
        [Test]
        public void The_destination_sites_header_should_be_set_to_the_given_sitekeys()
        {
            bus.SendToSites(new[] { "SiteA,SiteB" }, new TestMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Headers.ContainsKey(Headers.DestinationSites)), Arg<Address>.Is.Anything));
        }

        [Test]
        public void The_gateway_address_should_be_generated_based_on_the_master_node()
        {
            bus.SendToSites(new[] { "SiteA,SiteB" }, new TestMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Is.Anything, Arg<Address>.Is.Equal(gatewayAddress)));
        }
    }
}