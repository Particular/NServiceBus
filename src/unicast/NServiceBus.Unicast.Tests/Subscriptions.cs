namespace NServiceBus.Unicast.Tests
{
    using System;
    using NUnit.Framework;
    using Rhino.Mocks;
    using SomeUserNamespace;
    using Transport;

    [TestFixture]
    public class When_subscribing_to_messages : using_the_unicastbus
    {
        readonly Address addressToOwnerOfTestMessage = new Address("TestMessageOwner", "localhost");
        [SetUp]
        public void SetUp()
        {
            unicastBus.RegisterMessageType(typeof(TestMessage), addressToOwnerOfTestMessage);
        }
        [Test]
        public void Only_the_major_version_should_be_used_as_the_subscription_key_in_order_to_make_versioning_easier()
        {
            bus.Subscribe<TestMessage>();

            var version = typeof(TestMessage).Assembly.GetName().Version.Major + ".0.0.0";

            messageSender.AssertWasCalled(x =>
                x.Send(Arg<TransportMessage>.Matches(
                    m => m.Headers.ContainsKey(UnicastBus.SubscriptionMessageType) && m.Headers[UnicastBus.SubscriptionMessageType].Contains("Version=" + version)),
                    Arg<Address>.Is.Equal(addressToOwnerOfTestMessage)));

        }

        [Test]
        public void Should_set_the_message_intent_to_subscribe()
        {
            bus.Subscribe<TestMessage>();

            var version = typeof(TestMessage).Assembly.GetName().Version.Major + ".0.0.0";

            messageSender.AssertWasCalled(x =>
                x.Send(Arg<TransportMessage>.Matches(
                    m => m.MessageIntent == MessageIntentEnum.Subscribe),
                    Arg<Address>.Is.Equal(addressToOwnerOfTestMessage)));

        }
    }
    
    [TestFixture]
    public class When_subscribing_to_a_message_that_has_no_configured_address : using_the_unicastbus
    {
        [Test]
        public void Should_throw()
        {
            Assert.Throws<InvalidOperationException>(() => bus.Subscribe<EventMessage>());
        }
    }

    [TestFixture]
    public class When_unsubscribing_to_a_message_that_has_no_configured_address : using_the_unicastbus
    {
        [Test]
        public void Should_throw()
        {
            Assert.Throws<InvalidOperationException>(() => bus.Unsubscribe<EventMessage>());
        }
    }
    [TestFixture]
    public class When_subscribing_to_command_messages : using_the_unicastbus
    {
        [Test]
        public void Should_get_an_error_messages()
        {
            RegisterMessageType<CommandMessage>();
            Assert.Throws<InvalidOperationException>(() => bus.Subscribe<CommandMessage>());
        }
    }

    [TestFixture]
    public class When_unsubscribing_to_command_messages : using_the_unicastbus
    {
        [Test]
        public void Should_get_an_error_messages()
        {
            RegisterMessageType<CommandMessage>();
            Assert.Throws<InvalidOperationException>(() => bus.Unsubscribe<CommandMessage>());
        }
    }
}
