namespace NServiceBus.Unicast.Tests
{
    using System;
    using Contexts;
    using NUnit.Framework;

    [TestFixture]
    public class When_sending_a_message_in_send_only_mode : using_a_configured_unicastBus
    {
        [Test]
        public void Should_be_allowed()
        {
            Configure.Endpoint.AsSendOnly();
            RegisterMessageType<TestMessage>();
            bus.Send(Address.Local, new TestMessage());
        }
    }
    [TestFixture]
    public class When_subscribing_to_a_message_in_send_only_mode : using_a_configured_unicastBus
    {
        [Test]
        public void Should_not_be_allowed()
        {
            Configure.Endpoint.AsSendOnly(); 
            RegisterMessageType<TestMessage>();
            Assert.Throws<InvalidOperationException>(() => bus.Subscribe<TestMessage>());
        }
    }

    [TestFixture]
    public class When_unsubscribing_to_a_message_in_send_only_mode : using_a_configured_unicastBus
    {
        [Test]
        public void Should_not_be_allowed()
        {
            Configure.Endpoint.AsSendOnly();

            RegisterMessageType<TestMessage>();
            Assert.Throws<InvalidOperationException>(() => bus.Unsubscribe<TestMessage>());
        }
    }
    [TestFixture]
    public class When_replying_to_a_message_that_was_sent_with_null_reply_to_address : using_the_unicastBus
    {
        [Test]
        public void Should_blow()
        {
            RegisterMessageType<TestMessage>();
            var receivedMessage = Helpers.Helpers.Serialize(new TestMessage());
            RegisterMessageHandlerType<HandlerThatRepliesWithACommandToAMessage>();
            ReceiveMessage(receivedMessage);
            Assert.IsInstanceOf<InvalidOperationException>(ResultingException.GetBaseException());
        }
    }
    [TestFixture]
    public class When_returning_to_a_message_that_was_sent_with_null_reply_to_address : using_the_unicastBus
    {
        [Test]
        public void Should_blow()
        {
            RegisterMessageType<TestMessage>();
            var receivedMessage = Helpers.Helpers.Serialize(new TestMessage());
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