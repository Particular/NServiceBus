namespace NServiceBus.Gateway.Tests.Idempotency.Raven
{
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class When_storing_a_new_message : in_the_raven_storage
    {
        [Test]
        public void Should_store_the_correct_values()
        {
            var message = CreateTestMessage();

            Store(message);

            var messageStored = GetStoredMessage(message.ClientId);

            Assert.AreEqual(messageStored.TimeReceived, message.TimeReceived);
            Assert.AreEqual(messageStored.Headers.Count(), message.Headers.Count());
            Assert.AreEqual(messageStored.OriginalMessage, message.OriginalMessage);
        }

        [Test]
        public void Should_return_true_to_notify_that_the_message_was_stored()
        {
            var message = CreateTestMessage();

            Assert.True(Store(message));
        }
    }
}