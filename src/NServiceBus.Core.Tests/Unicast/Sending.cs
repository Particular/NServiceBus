﻿namespace NServiceBus.Unicast.Tests
{
    using System;
    using Contexts;
    using NUnit.Framework;
    using Rhino.Mocks;

    [TestFixture]
    public class When_sending_a_message_with_databusProperty : using_the_unicastBus
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
    public class When_sending_a_event_message : using_the_unicastBus
    {
        [Test]
        public void Should_get_an_error_messages()
        {
            RegisterMessageType<EventMessage>();
            Assert.Throws<InvalidOperationException>(() => bus.Send(new EventMessage()));
        }
    }

    [TestFixture]
    public class When_sending_a_event_message_to_sites : using_the_unicastBus
    {
        [Test]
        public void Should_get_an_error_messages()
        {
            RegisterMessageType<EventMessage>();
            Assert.Throws<InvalidOperationException>(() => bus.SendToSites(new[] { "KeyA" }, new EventMessage()));
        }
    }

    [TestFixture]
    public class When_sending_messages_to_sites : using_the_unicastBus
    {
        [Test]
        public void The_destination_sites_header_should_be_set_to_the_given_siteKeys()
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
    public class When_sending_any_message : using_the_unicastBus
    {
        [Test]
        public void The_content_type_should_be_set()
        {
            RegisterMessageType<TestMessage>();
            bus.Send(new TestMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Headers[Headers.ContentType] == "text/xml"), Arg<Address>.Is.Anything));
        }

        [Test]
        public void It_should_be_persistent_by_default()
        {
            RegisterMessageType<TestMessage>();
            bus.Send(new TestMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Recoverable), Arg<Address>.Is.Anything));
        }

        [Test]
        public void Should_set_the_reply_to_address()
        {
            RegisterMessageType<TestMessage>();
            bus.Send(new TestMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.ReplyToAddress == Address.Local), Arg<Address>.Is.Anything));
        }

        [Test]
        public void Should_generate_a_conversation_id()
        {
            RegisterMessageType<TestMessage>();
            bus.Send(new TestMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Headers.ContainsKey(Headers.ConversationId)), Arg<Address>.Is.Anything));
        }

        [Test]
        public void Should_not_override_a_conversation_id_specified_by_the_user()
        {
            RegisterMessageType<TestMessage>();


            bus.Send<TestMessage>(m=>m.SetHeader(Headers.ConversationId,"my order id"));

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Headers[Headers.ConversationId] == "my order id"), Arg<Address>.Is.Anything));
        }

        [Test, Ignore("Needs refactoring to make testing possible")]
        public void Should_propagate_the_incoming_replyTo_address_if_requested()
        {
            var addressOfIncomingMessage = Address.Parse("Incoming");

            //todo - add a way to set the context from out tests

            unicastBus.PropagateReturnAddressOnSend = true;
            RegisterMessageType<TestMessage>();
            bus.Send(new TestMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.ReplyToAddress == addressOfIncomingMessage), Arg<Address>.Is.Anything));
        }
    }

    [TestFixture]
    public class When_sending_multiple_messages_in_one_go : using_the_unicastBus
    {

        [Test]
        public void Should_be_persistent_if_any_of_the_messages_is_persistent()
        {
            RegisterMessageType<NonPersistentMessage>();
            RegisterMessageType<PersistentMessage>();
            bus.Send(new NonPersistentMessage(), new PersistentMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Recoverable), Arg<Address>.Is.Anything));
        }


        [Test]
        public void Should_use_the_lowest_time_to_be_received()
        {
            RegisterMessageType<NonPersistentMessage>();
            RegisterMessageType<PersistentMessage>();
            bus.Send(new NonPersistentMessage(), new PersistentMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.TimeToBeReceived == TimeSpan.FromMinutes(45)), Arg<Address>.Is.Anything));
        }

        [Test]
        public void Should_use_the_address_of_the_first_message()
        {
            var firstAddress = Address.Parse("first");
            var secondAddress = Address.Parse("second");
            RegisterMessageType<NonPersistentMessage>(firstAddress);
            RegisterMessageType<PersistentMessage>(secondAddress);
            bus.Send(new NonPersistentMessage(), new PersistentMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Recoverable), Arg<Address>.Is.Equal(firstAddress)));
        }


        [TimeToBeReceived("00:45:00")]
        class PersistentMessage { }

        [Express]
        class NonPersistentMessage { }
    }



    [TestFixture]
    public class When_sending_any_message_from_a_volatile_endpoint : using_the_unicastBus
    {
        [Test]
        public void It_should_be_non_persistent_by_default()
        {
            MessageMetadataRegistry.DefaultToNonPersistentMessages = true;
            RegisterMessageType<TestMessage>();
            bus.Send(new TestMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => !m.Recoverable), Arg<Address>.Is.Anything));
        }
    }

    [TestFixture]
    public class When_sending_a_message_that_has_no_configured_address : using_the_unicastBus
    {
        [Test]
        public void Should_throw()
        {
            Assert.Throws<InvalidOperationException>(() => bus.Send(new CommandMessage()));
        }
    }

    [TestFixture]
    public class When_sending_a_command_message : using_the_unicastBus
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
    public class When_sending_a_interface_message : using_the_unicastBus
    {
        [Test]
        public void Should_specify_the_message_to_be_recoverable()
        {
            var defaultAddress = RegisterMessageType<InterfaceMessage>();

            bus.Send<InterfaceMessage>(m => { });

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Recoverable), Arg<Address>.Is.Equal(defaultAddress)));
        }
    }

    [TestFixture]
    public class When_raising_an_in_memory_message : using_the_unicastBus
    {
        [Test]
        public void Should_invoke_registered_message_handlers()
        {
            RegisterMessageType<TestMessage>();

            RegisterMessageHandlerType<TestMessageHandler1>();
            RegisterMessageHandlerType<TestMessageHandler2>();

            bus.InMemory.Raise(new TestMessage());

            Assert.True(TestMessageHandler1.Called);
            Assert.True(TestMessageHandler2.Called);
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
