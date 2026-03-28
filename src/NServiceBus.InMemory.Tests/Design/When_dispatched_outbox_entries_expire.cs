namespace NServiceBus.Persistence.InMemory;

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Outbox;

[TestFixture]
public class When_dispatched_outbox_entries_expire
{
    [Test]
    public async Task Should_expire_from_dispatch_time()
    {
        var storage = new InMemoryOutboxStorage();
        var message = new OutboxMessage("message-id",
        [
            new TransportOperation("operation-id", [], new byte[] { 1 }, [])
        ]);

        await using (var transaction = (InMemoryOutboxTransaction)await storage.BeginTransaction(new(), TestContext.CurrentContext.CancellationToken))
        {
            await storage.Store(message, transaction, new(), TestContext.CurrentContext.CancellationToken);
            await transaction.Commit(TestContext.CurrentContext.CancellationToken);
        }

        storage.Messages[message.MessageId].StoredAt = DateTime.UtcNow.AddDays(-1);

        await storage.SetAsDispatched(message.MessageId, new(), TestContext.CurrentContext.CancellationToken);

        storage.RemoveEntriesOlderThan(DateTime.UtcNow.AddMinutes(-5));

        var persisted = await storage.Get(message.MessageId, new(), TestContext.CurrentContext.CancellationToken);
        Assert.That(persisted, Is.Not.Null);
    }
}