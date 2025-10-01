namespace NServiceBus.PersistenceTesting.Outbox;

using System;
using System.Threading.Tasks;
using NServiceBus.Outbox;
using NUnit.Framework;

[TestFixtureSource(typeof(PersistenceTestsConfiguration), nameof(PersistenceTestsConfiguration.OutboxVariants))]
public class OutboxStorageTests(TestVariant param)
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        configuration = new PersistenceTestsConfiguration(param);
        await configuration.Configure();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await configuration.Cleanup();
    }

    [Test]
    public async Task Should_find_existing_outbox_data()
    {
        configuration.RequiresOutboxSupport();

        var storage = configuration.OutboxStorage;
        var ctx = configuration.GetContextBagForOutbox();

        string messageId = Guid.NewGuid().ToString();
        _ = await storage.Get(messageId, ctx);

        string transportOperationMessageId = Guid.NewGuid().ToString();
        var messageToStore = new OutboxMessage(messageId, new[] { new TransportOperation(transportOperationMessageId, null, null, null) });
        await using (var transaction = await storage.BeginTransaction(ctx))
        {
            await storage.Store(messageToStore, transaction, ctx);

            await transaction.Commit();
        }

        var message = await storage.Get(messageId, configuration.GetContextBagForOutbox());

        Assert.That(message, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(message.MessageId, Is.EqualTo(messageId));
            Assert.That(message.TransportOperations, Has.Length.EqualTo(1));
        }
        Assert.That(message.TransportOperations[0].MessageId, Is.EqualTo(transportOperationMessageId));
    }

    [Test]
    public async Task Should_clear_operations_on_dispatched_messages()
    {
        configuration.RequiresOutboxSupport();

        var storage = configuration.OutboxStorage;
        var ctx = configuration.GetContextBagForOutbox();

        var messageId = Guid.NewGuid().ToString();
        _ = await storage.Get(messageId, ctx);

        var messageToStore = new OutboxMessage(messageId, new[] { new TransportOperation("x", null, null, null) });
        await using (var transaction = await storage.BeginTransaction(ctx))
        {
            await storage.Store(messageToStore, transaction, ctx);

            await transaction.Commit();
        }

        await storage.SetAsDispatched(messageId, ctx);

        var message = await storage.Get(messageId, configuration.GetContextBagForOutbox());

        Assert.That(message, Is.Not.Null);
        Assert.That(message.TransportOperations, Is.Empty);
    }

    [Test]
    public async Task Should_throw_if_trying_to_insert_same_messageid()
    {
        configuration.RequiresOutboxSupport();

        var storage = configuration.OutboxStorage;
        var winningContextBag = configuration.GetContextBagForOutbox();
        var losingContextBag = configuration.GetContextBagForOutbox();
        _ = await storage.Get("MySpecialId", winningContextBag);
        _ = await storage.Get("MySpecialId", losingContextBag);

        await using (var transactionA = await storage.BeginTransaction(winningContextBag))
        {
            await storage.Store(new OutboxMessage("MySpecialId", []), transactionA, winningContextBag);
            await transactionA.Commit();
        }

        Assert.That(async () =>
        {
            await using (var transactionB = await storage.BeginTransaction(losingContextBag))
            {
                await storage.Store(new OutboxMessage("MySpecialId", []),
                    transactionB, losingContextBag);
                await transactionB.Commit();
            }
        }, Throws.Exception);
    }

    [Test]
    public async Task Should_not_store_when_transaction_not_committed()
    {
        configuration.RequiresOutboxSupport();

        var storage = configuration.OutboxStorage;
        var ctx = configuration.GetContextBagForOutbox();

        var messageId = Guid.NewGuid().ToString();
        _ = await storage.Get(messageId, ctx);

        await using (var transaction = await storage.BeginTransaction(ctx))
        {
            var messageToStore = new OutboxMessage(messageId, [new TransportOperation("x", null, null, null)]);
            await storage.Store(messageToStore, transaction, ctx);

            // do not commit
        }

        var message = await storage.Get(messageId, configuration.GetContextBagForOutbox());
        Assert.That(message, Is.Null);
    }

    [Test]
    public async Task Should_store_when_transaction_committed()
    {
        configuration.RequiresOutboxSupport();

        var storage = configuration.OutboxStorage;
        var ctx = configuration.GetContextBagForOutbox();

        var messageId = Guid.NewGuid().ToString();
        _ = await storage.Get(messageId, ctx);

        await using (var transaction = await storage.BeginTransaction(ctx))
        {
            var messageToStore = new OutboxMessage(messageId, [new TransportOperation("x", null, null, null)]);
            await storage.Store(messageToStore, transaction, ctx);

            await transaction.Commit();
        }

        var message = await storage.Get(messageId, configuration.GetContextBagForOutbox());
        Assert.That(message, Is.Not.Null);
    }

    PersistenceTestsConfiguration configuration;
    readonly TestVariant param = param.DeepCopy();
}