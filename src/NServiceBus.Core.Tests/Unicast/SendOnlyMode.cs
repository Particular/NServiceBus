namespace NServiceBus.Unicast.Tests
{
    using System;
    using Contexts;
    using NUnit.Framework;

    [TestFixture]
    class When_sending_a_message_in_send_only_mode : using_a_configured_unicastBus
    {
        [Test]
        public void Should_be_allowed()
        {
            settings.Set("Endpoint.SendOnly", true);
            RegisterMessageType<TestMessage>();
            bus.Send(Address.Local, new TestMessage());
        }
    }
    [TestFixture]
    class When_subscribing_to_a_message_in_send_only_mode : using_a_configured_unicastBus
    {
        [Test]
        public void Should_not_be_allowed()
        {
            settings.Set("Endpoint.SendOnly", true);
            RegisterMessageType<TestMessage>();
            Assert.Throws<InvalidOperationException>(() => bus.Subscribe<TestMessage>());
        }
    }

    [TestFixture]
    class When_unsubscribing_to_a_message_in_send_only_mode : using_a_configured_unicastBus
    {
        [Test]
        public void Should_not_be_allowed()
        {
            settings.Set("Endpoint.SendOnly", true);

            RegisterMessageType<TestMessage>();
            Assert.Throws<InvalidOperationException>(() => bus.Unsubscribe<TestMessage>());
        }
    }

    [TestFixture]
    class When_replying_to_a_message_that_was_sent_with_null_reply_to_address : using_the_unicastBus
    {
        [Test]
        public void Should_blow()
        {
            RegisterMessageType<TestMessage>();
            var receivedMessage = Helpers.Helpers.Serialize(new TestMessage(),true);
            RegisterMessageHandlerType<HandlerThatRepliesWithACommandToAMessage>();
            ReceiveMessage(receivedMessage);
            Assert.IsInstanceOf<InvalidOperationException>(ResultingException.GetBaseException());
        }
    }

    [TestFixture]
    class When_returning_to_a_message_that_was_sent_with_null_reply_to_address : using_the_unicastBus
    {
        [Test]
        public void Should_blow()
        {
            RegisterMessageType<TestMessage>();
            var receivedMessage = Helpers.Helpers.Serialize(new TestMessage(),true);
            RegisterMessageHandlerType<HandlerThatReturns>();
            ReceiveMessage(receivedMessage);
            Assert.IsInstanceOf<InvalidOperationException>(ResultingException.GetBaseException());
        }
    }

    public class TestMessage : IMessage
    {
    }
    
    class HandlerThatRepliesWithACommandToAMessage : IHandleMessages<TestMessage>
    {
        public IBus Bus { get; set; }

        public void Handle(TestMessage message)
        {
            Bus.Reply(new TestMessage());
        }
    }

    class HandlerThatReturns : IHandleMessages<TestMessage>
    {
        public IBus Bus { get; set; }

        public void Handle(TestMessage message)
        {
            Bus.Return(1);
        }
    }

}