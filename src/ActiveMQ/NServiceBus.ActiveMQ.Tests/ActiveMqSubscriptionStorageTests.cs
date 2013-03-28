namespace NServiceBus.Transports.ActiveMQ.Tests
{
    using FluentAssertions;
    using NServiceBus.Unicast.Subscriptions;
    using NUnit.Framework;
    using NServiceBus.Transports.ActiveMQ;

    [TestFixture]
    public class ActiveMqSubscriptionStorageTests
    {
        [Test]
        public void GetSubscriberAddressesForMessage_ShouldReturnTheLocalAddress()
        {
            Address.SetParser<ActiveMQAddress>();

            var testee = new ActiveMqSubscriptionStorage();

            var result = testee.GetSubscriberAddressesForMessage(new[] { new MessageType("SomeType", "1.0.0.0") });

            result.Should().Equal(Address.Local);
        }
    }
}