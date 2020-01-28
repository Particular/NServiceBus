namespace NServiceBus.Persistence.InMemory.Tests
{
    using NUnit.Framework;

    [TestFixture]
    class ClientIdStorageTests
    {
        [Test]
        public void Should_detect_duplicates()
        {
            var clientIdStorage = new ClientIdStorage(10);

            clientIdStorage.RegisterClientId("A");
            clientIdStorage.RegisterClientId("B");

            Assert.True(clientIdStorage.IsDuplicate("A"));
            Assert.False(clientIdStorage.IsDuplicate("C"));
        }

        [Test]
        public void Should_evict_oldest_entry_when_LRU_reaches_limit()
        {
            var clientIdStorage = new ClientIdStorage(2);

            clientIdStorage.RegisterClientId("A");
            clientIdStorage.RegisterClientId("B");
            clientIdStorage.RegisterClientId("C");

            Assert.False(clientIdStorage.IsDuplicate("A"));
        }

        [Test]
        public void Should_reset_time_added_for_existing_IDs_when_checked()
        {
            var clientIdStorage = new ClientIdStorage(2);

            clientIdStorage.RegisterClientId("A");
            clientIdStorage.RegisterClientId("B");

            Assert.True(clientIdStorage.IsDuplicate("A"));

            clientIdStorage.RegisterClientId("C");

            Assert.False(clientIdStorage.IsDuplicate("B"));
            Assert.True(clientIdStorage.IsDuplicate("A"));
        }
    }
}
