namespace NServiceBus.GatewayPersister.NHibernate.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class When_storing_a_duplicate_message : BaseStorage
    {
        [Test]
        public void Should_return_false_to_notify_that_the_message_is_already_received()
        {
            var message = CreateTestMessage();

            Store(message);

            Assert.False(Store(message));
        }
    }
}