namespace NServiceBus.Unicast.Tests
{
    using System;
    using Contexts;
    using NUnit.Framework;
    using Rhino.Mocks;
    using Transport;

    [TestFixture]
    public class When_sending_a_message_with_databusproperty : using_the_unicastbus
    {
        [Test]
        public void Should_throw_if_more_than_one_is_sent_in_the_same_send()
        {
            RegisterMessageType<CommandWithDataBusPropertyMessage>();

            Assert.Throws<InvalidOperationException>(() => bus.Send(new CommandWithDataBusPropertyMessage(), new CommandWithDataBusPropertyMessage()));
        }

        [Test]
        public void Should_sent_if_only_one_message_is_in_the_same_send()
        {
            RegisterMessageType<CommandWithDataBusPropertyMessage>();

            bus.Send(new CommandWithDataBusPropertyMessage());
        }
    }

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
            RegisterMessageType<TestMessage>();
            bus.SendToSites(new[] { "SiteA,SiteB" }, new TestMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Headers.ContainsKey(Headers.DestinationSites)), Arg<Address>.Is.Anything));
        }

        [Test]
        public void The_gateway_address_should_be_generated_based_on_the_master_node()
        {
            RegisterMessageType<TestMessage>();
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

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Recoverable), Arg<Address>.Is.Anything));
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

    [TestFixture]
    public class When_raising_an_in_memory_message : using_the_unicastbus
    {
        [Test]
        public void Should_invoke_registered_message_handlers()
        {
            RegisterMessageType<TestMessage>();

            RegisterMessageHandlerType<TestMessageHandler1>();
            RegisterMessageHandlerType<TestMessageHandler2>();

            bus.InMemory.Raise(new TestMessage());

            Assert.That(TestMessageHandler1.Called, Is.True);
            Assert.That(TestMessageHandler2.Called, Is.True);
        }

        public class TestMessageHandler1 : IHandleMessages<TestMessage>
        {
            public static bool Called;

            public void Handle(TestMessage message)
            {
                Called = true;
            }
        }

        public class TestMessageHandler2 : IHandleMessages<TestMessage>
        {
            public static bool Called;

            public void Handle(TestMessage message)
            {
                Called = true;
            }
        }
    }
}
