namespace NServiceBus.Unicast.Tests
{
    using System;
    using Contexts;
    using NUnit.Framework;
    using Rhino.Mocks;

    [TestFixture]
    public class When_subscribing_to_messages : using_the_unicastbus
    {
        readonly Address addressToOwnerOfTestMessage = new Address("TestMessageOwner", "localhost");
        /// <summary>
        /// Set Up
        /// </summary>
        [SetUp]
        public new void SetUp()
        {
            router.RegisterRoute(typeof(TestMessage), addressToOwnerOfTestMessage);
        }
        [Test]
        public void Should_send_the_assemblyqualified_name_as_subscription_type()
        {
            bus.Subscribe<TestMessage>();

            AssertSubscription(m => m.Headers.ContainsKey(Headers.SubscriptionMessageType) &&
                                    m.Headers[Headers.SubscriptionMessageType] == typeof(TestMessage).AssemblyQualifiedName,
                                addressToOwnerOfTestMessage);

        }

        [Test]
        public void Should_set_the_message_intent_to_subscribe()
        {
            bus.Subscribe<TestMessage>();

            AssertSubscription(m => m.MessageIntent == MessageIntentEnum.Subscribe,
                                addressToOwnerOfTestMessage);
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

    [TestFixture]
    public class When_creating_message_types
    {
        [Test]
        public void Should_parse_types()
        {
            var messageType = new Subscriptions.MessageType(typeof(TestMessage));

            Assert.AreEqual(messageType.TypeName, typeof(TestMessage).FullName);
            Assert.AreEqual(messageType.Version, typeof(TestMessage).Assembly.GetName().Version);
        }

        [Test]
        public void Should_parse_AssemblyQualifiedName()
        {
            var messageType = new Subscriptions.MessageType(typeof(TestMessage).AssemblyQualifiedName);

            Assert.AreEqual(messageType.TypeName, typeof(TestMessage).FullName);
            Assert.AreEqual(messageType.Version, typeof(TestMessage).Assembly.GetName().Version);
        }

        [Test]
        public void Should_parse_version_strings()
        {
            var messageType = new Subscriptions.MessageType("TestMessage", "1.2.3.4");

            Assert.AreEqual(messageType.TypeName, "TestMessage");
            Assert.AreEqual(messageType.Version, new Version(1, 2, 3, 4));
        }


        class TestMessage
        {

        }
    }
}
