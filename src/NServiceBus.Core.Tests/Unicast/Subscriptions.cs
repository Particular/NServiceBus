namespace NServiceBus.Unicast.Tests
{
    using System;
    using Contexts;
    using Core.Tests.Fakes;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    public class When_subscribing_to_messages : using_the_unicastBus
    {
        readonly Address addressToOwnerOfTestMessage = new Address("TestMessageOwner", "localhost");
        /// <summary>
        /// Set Up
        /// </summary>
        [SetUp]
        public new void SetUp()
        {
            SettingsHolder.Set("NServiceBus.Transport.SelectedTransport", new Msmq());
            router.RegisterMessageRoute(typeof(TestMessage), addressToOwnerOfTestMessage);
            
        }
        [Test]
        public void Should_send_the_assemblyQualified_name_as_subscription_type()
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
    public class When_subscribing_to_a_message_that_has_no_configured_address : using_the_unicastBus
    {
        [Test]
        public void Should_throw_when_not_using_a_centralized_pub_sub_transport()
        {
            SettingsHolder.Set("NServiceBus.Transport.SelectedTransport", new Msmq());
            Assert.Throws<InvalidOperationException>(() => bus.Subscribe<EventMessage>());
        }

        [Test]
        public void Should_not_throw_when_using_a_centralized_pub_sub_transport()
        {
            SettingsHolder.Set("NServiceBus.Transport.SelectedTransport", new FakeCentralizedPubSubTransportDefinition() );
            Assert.DoesNotThrow(() => bus.Subscribe<EventMessage>());
        }
    }

    [TestFixture]
    public class When_unsubscribing_to_a_message_that_has_no_configured_address : using_the_unicastBus
    {
        [Test]
        public void Should_throw_when_not_using_a_centralized_pub_sub_transport()
        {
            SettingsHolder.Set("NServiceBus.Transport.SelectedTransport", new Msmq());
            Assert.Throws<InvalidOperationException>(() => bus.Unsubscribe<EventMessage>());
        }

        [Test]
        public void Should_not_throw_when_using_a_centralized_pub_sub_transport()
        {
            SettingsHolder.Set("NServiceBus.Transport.SelectedTransport", new FakeCentralizedPubSubTransportDefinition());
            Assert.DoesNotThrow(() => bus.Unsubscribe<EventMessage>());
        }
    }
    [TestFixture]
    public class When_subscribing_to_command_messages : using_the_unicastBus
    {
        [Test]
        public void Should_get_an_error_messages()
        {
            RegisterMessageType<CommandMessage>();
            Assert.Throws<InvalidOperationException>(() => bus.Subscribe<CommandMessage>());
        }
    }

    [TestFixture]
    public class When_unsubscribing_to_command_messages : using_the_unicastBus
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
