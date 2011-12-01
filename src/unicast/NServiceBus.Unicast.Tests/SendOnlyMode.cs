namespace NServiceBus.Unicast.Tests
{
    using System;
    using Contexts;
    using NServiceBus;
    using NUnit.Framework;

    [TestFixture]
    public class When_sending_a_message_in_send_only_mode : using_a_configured_unicastbus
    {
        [Test]
        public void Should_be_allowed()
        {
            RegisterMessageType<TestMessage>();
            bus.Send(Address.Local, new TestMessage());
        }
    }
    [TestFixture]
    public class When_subscribing_to_a_message_in_send_only_mode : using_a_configured_unicastbus
    {
        [Test]
        public void Should_not_be_allowed()
        {
            RegisterMessageType<TestMessage>();
            Assert.Throws<InvalidOperationException>(() => bus.Subscribe<TestMessage>());
        }
    }

    [TestFixture]
    public class When_unsubscribing_to_a_message_in_send_only_mode : using_a_configured_unicastbus
    {
        [Test]
        public void Should_not_be_allowed()
        {
            RegisterMessageType<TestMessage>();
            Assert.Throws<InvalidOperationException>(() => bus.Unsubscribe<TestMessage>());
        }
    }
    public class TestMessage : IMessage
    {
    }
}