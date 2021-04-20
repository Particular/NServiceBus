﻿namespace NServiceBus.Unicast.Tests
{
    using System;
    using Contexts;
    using NUnit.Framework;
    using Rhino.Mocks;

    [TestFixture]
    class When_sending_a_message_with_databusProperty : using_the_unicastBus
    {
        [Test]
        public void Should_sent_if_only_one_message_is_in_the_same_send()
        {
            RegisterMessageType<CommandWithDataBusPropertyMessage>();

            bus.Send(new CommandWithDataBusPropertyMessage());
        }
    }

    [TestFixture]
    class When_sending_a_event_message : using_the_unicastBus
    {
        [Test]
        public void Should_get_an_error_messages()
        {
            RegisterMessageType<EventMessage>();
            Assert.Throws<InvalidOperationException>(() => bus.Send(new EventMessage()));
        }
    }

    [TestFixture]
    class When_sending_any_message : using_the_unicastBus
    {
        [Test]
        public void The_content_type_should_be_set()
        {
            bus.OutgoingHeaders["MyStaticHeader"] = "StaticHeaderValue";
            RegisterMessageType<TestMessage>();
            bus.Send(new TestMessage());


            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m =>
                m.Headers[Headers.ContentType] == "text/xml" &&
                m.Headers["MyStaticHeader"] == "StaticHeaderValue"), Arg<SendOptions>.Is.Anything));
        }

        [Test]
        public void It_should_be_persistent_by_default()
        {
            RegisterMessageType<TestMessage>();
            bus.Send(new TestMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Recoverable), Arg<SendOptions>.Is.Anything));
        }

        [Test]
        public void Should_set_the_reply_to_address()
        {
            RegisterMessageType<TestMessage>();
            bus.Send(new TestMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Is.Anything, Arg<SendOptions>.Matches(o => o.ReplyToAddress == configure.LocalAddress)));
        }

        [Test]
        public void Should_generate_a_conversation_id()
        {
            RegisterMessageType<TestMessage>();
            bus.Send(new TestMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Headers.ContainsKey(Headers.ConversationId)), Arg<SendOptions>.Is.Anything));
        }

        [Test]
        public void Should_not_override_a_conversation_id_specified_by_the_user()
        {
            RegisterMessageType<TestMessage>();


            bus.Send<TestMessage>(m => bus.SetMessageHeader(m, Headers.ConversationId, "my order id"));

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Headers[Headers.ConversationId] == "my order id"), Arg<SendOptions>.Is.Anything));
        }

        [Test, Ignore("Needs refactoring to make testing possible")]
        public void Should_propagate_the_incoming_replyTo_address_if_requested()
        {
            var addressOfIncomingMessage = Address.Parse("Incoming");

            //todo - add a way to set the context from out tests

            bus.PropagateReturnAddressOnSend = true;
            RegisterMessageType<TestMessage>();
            bus.Send(new TestMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.ReplyToAddress == addressOfIncomingMessage), Arg<SendOptions>.Is.Anything));
        }
    }

    [TestFixture]
    class When_sending_multiple_messages_in_one_go : using_the_unicastBus
    {

        [Test]
        public void Should_be_persistent_if_any_of_the_messages_is_persistent()
        {
            RegisterMessageType<NonPersistentMessage>(configure.LocalAddress);
            RegisterMessageType<PersistentMessage>(configure.LocalAddress);
            bus.Send(new NonPersistentMessage());
            bus.Send(new PersistentMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Recoverable), Arg<SendOptions>.Is.Anything));
        }


        [Test]
        public void Should_use_the_lowest_time_to_be_received()
        {
            RegisterMessageType<NonPersistentMessage>(configure.LocalAddress);
            RegisterMessageType<PersistentMessage>(configure.LocalAddress);
            bus.Send(new NonPersistentMessage());
            bus.Send(new PersistentMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.TimeToBeReceived == TimeSpan.FromMinutes(45)), Arg<SendOptions>.Is.Anything));
        }

        [TimeToBeReceived("00:45:00")]
        class PersistentMessage { }

        [Express]
        class NonPersistentMessage { }
    }



    [TestFixture]
    class When_sending_any_message_from_a_volatile_endpoint : using_the_unicastBus
    {
        [Test]
        public void It_should_be_non_persistent_by_default()
        {
            MessageMetadataRegistry.DefaultToNonPersistentMessages = true;
            RegisterMessageType<TestMessage>();
            bus.Send(new TestMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => !m.Recoverable), Arg<SendOptions>.Is.Anything));
        }
    }

    [TestFixture]
    class When_sending_a_command_message : using_the_unicastBus
    {
        [Test]
        public void Should_specify_the_message_to_be_recoverable()
        {
            RegisterMessageType<CommandMessage>();

            bus.Send(new CommandMessage());

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Recoverable), Arg<SendOptions>.Is.Anything));
        }
    }

    [TestFixture]
    class When_sending_a_interface_message : using_the_unicastBus
    {
        [Test]
        public void Should_specify_the_message_to_be_recoverable()
        {
            var defaultAddress = RegisterMessageType<InterfaceMessage>();

            bus.Send<InterfaceMessage>(m => { });

            messageSender.AssertWasCalled(x => x.Send(Arg<TransportMessage>.Matches(m => m.Recoverable), Arg<SendOptions>.Matches(o=>o.Destination == defaultAddress)));
        }
    }
}