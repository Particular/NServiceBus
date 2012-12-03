namespace NServiceBus.ActiveMQ
{
    using FluentAssertions;

    using NServiceBus.Unicast.Subscriptions;

    using NUnit.Framework;

    [TestFixture]
    public class ActiveMqSubscriptionStorageTests
    {
        [Test]
        public void GetSubscriberAddressesForMessage_ShouldReturnTheLocalAddress()
        {
            var testee = new ActiveMqSubscriptionStorage();

            var result = testee.GetSubscriberAddressesForMessage(new[] { new MessageType("SomeType", "1.0.0.0") });

            result.Should().Equal(Address.Local);
        }
    }
}