namespace NServiceBus.Persistence.InMemory.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Extensibility;
    using NServiceBus.InMemory.Outbox;
    using NUnit.Framework;
    using Outbox;

    [TestFixture]
    class InMemoryOutboxPersisterTests
    {
    
        [Test]
        public void Should_clear_operations_on_dispatched_messages()
        {
            var storage = new InMemoryOutboxStorage();

            var messageId = "myId";
    
            storage.Store(messageId,new List<TransportOperation>{new TransportOperation("x",null,null,null)}, new OutboxStorageOptions(new ContextBag()));

            OutboxMessage message;

            storage.SetAsDispatched(messageId, new OutboxStorageOptions(new ContextBag()));

            storage.TryGet(messageId, new OutboxStorageOptions(new ContextBag()), out message);


            Assert.False(message.TransportOperations.Any());
        }


        [Test]
        public void Should_not_remove_non_dispatched_messages()
        {
            var storage = new InMemoryOutboxStorage();

            var messageId = "myId";

            storage.Store(messageId, new List<TransportOperation> { new TransportOperation("x", null, null, null) }, new OutboxStorageOptions(new ContextBag()));

            OutboxMessage message;

            storage.RemoveEntriesOlderThan(DateTime.UtcNow);

            Assert.True(storage.TryGet(messageId, new OutboxStorageOptions(new ContextBag()), out message));
        }


        [Test]
        public void Should_clear_dispatched_messages_after_given_expiry()
        {
            var storage = new InMemoryOutboxStorage();

            var messageId = "myId";

            var beforeStore = DateTime.UtcNow;

            storage.Store(messageId, new List<TransportOperation> { new TransportOperation("x", null, null, null) }, new OutboxStorageOptions(new ContextBag()));

            OutboxMessage message;

            storage.SetAsDispatched(messageId, new OutboxStorageOptions(new ContextBag()));

            storage.RemoveEntriesOlderThan(beforeStore);
            
            Assert.True(storage.TryGet(messageId, new OutboxStorageOptions(new ContextBag()), out message));

            storage.RemoveEntriesOlderThan(DateTime.UtcNow);

            Assert.False(storage.TryGet(messageId, new OutboxStorageOptions(new ContextBag()), out message));
        }
    }
}