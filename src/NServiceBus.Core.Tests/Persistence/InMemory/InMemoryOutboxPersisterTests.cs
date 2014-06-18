namespace NServiceBus.Persistence.InMemory.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
    
            storage.Store(messageId,new List<TransportOperation>{new TransportOperation("x",null,null,null)});

            OutboxMessage message;

            storage.SetAsDispatched(messageId);

            storage.TryGet(messageId, out message);


            Assert.False(message.TransportOperations.Any());
        }



        [Test]
        public void Should_not_remove_non_dispatched_messages()
        {
            var storage = new InMemoryOutboxStorage();

            var messageId = "myId";

            storage.Store(messageId, new List<TransportOperation> { new TransportOperation("x", null, null, null) });

            OutboxMessage message;

            storage.RemoveEntriesOlderThan(DateTime.UtcNow);

            Assert.True(storage.TryGet(messageId, out message));
        }



        [Test]
        public void Should_clear_dispatched_messages_after_given_expiry()
        {
            var storage = new InMemoryOutboxStorage();

            var messageId = "myId";

            var beforeStore = DateTime.UtcNow;

            storage.Store(messageId, new List<TransportOperation> { new TransportOperation("x", null, null, null) });

            OutboxMessage message;

            storage.SetAsDispatched(messageId);

            storage.RemoveEntriesOlderThan(beforeStore);
            
            Assert.True(storage.TryGet(messageId, out message));

            storage.RemoveEntriesOlderThan(DateTime.UtcNow);

            Assert.False(storage.TryGet(messageId, out message));
        }
    }
}