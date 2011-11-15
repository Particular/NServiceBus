﻿namespace NServiceBus.Unicast.Tests
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


    [TestFixture]
    public class When_sending_a_message_that_has_no_configured_address : using_the_unicastbus
    {
        [Test]
        public void Should_throw()
        {
            Assert.Throws<InvalidOperationException>(()=>bus.Send(new CommandMessage()));
        }
    }

    [TestFixture]
    public class When_sending_a_command_message : using_the_unicastbus
    {
        [Test]
        public void Should_specify_the_message_to_be_recoverable()
        {
            RegisterMessageType<CommandMessage>();
            
            bus.Send(new CommandMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Recoverable == true), Arg<Address>.Is.Anything));
        }
    }


    [TestFixture]
    public class When_sending_a_interface_message : using_the_unicastbus
    {
        [Test]
        public void Should_specify_the_message_to_be_recoverable()
        {
            var defaultAddress = RegisterMessageType<InterfaceMessage>();

            bus.Send<InterfaceMessage>(m=>{});

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Recoverable), Arg<Address>.Is.Equal(defaultAddress)));
        }
    }
}