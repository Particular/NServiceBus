namespace NServiceBus.PersistenceTesting.Outbox;

using System;
using System.Collections.Generic;
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

    [Test]
    public async Task Should_return_fresh_header_dictionaries_from_get()
    {
        configuration.RequiresOutboxSupport();

        var storage = configuration.OutboxStorage;
        var ctx = configuration.GetContextBagForOutbox();

        var messageId = Guid.NewGuid().ToString();
        _ = await storage.Get(messageId, ctx); // dedup prime, per existing suite convention

        var operationId = Guid.NewGuid().ToString();
        var headers = new Dictionary<string, string>
        {
            { "HeaderPooling.Key1", "value1" },
            { "HeaderPooling.Key2", "value2" },
        };
        var messageToStore = new OutboxMessage(messageId,
            [new TransportOperation(operationId, null, new byte[] { 1, 2, 3 }, headers)]);

        await using (var transaction = await storage.BeginTransaction(ctx))
        {
            await storage.Store(messageToStore, transaction, ctx);

            await transaction.Commit();
        }

        var firstGet = await storage.Get(messageId, configuration.GetContextBagForOutbox());

        // Emulates the dispatch pipeline under header pooling: ImmediateDispatchTerminator returns every
        // dispatched operation's header dictionary to HeaderPool.Shared, which clears it. An outbox that
        // handed out a shared reference to stored state gets its stored entry wiped right here.
        foreach (var operation in firstGet.TransportOperations)
        {
            operation.Headers.Clear();
        }

        var secondGet = await storage.Get(messageId, configuration.GetContextBagForOutbox());

        Assert.That(secondGet, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(secondGet.TransportOperations, Has.Length.EqualTo(1));
            Assert.That(secondGet.TransportOperations[0].Headers, Is.Not.SameAs(firstGet.TransportOperations[0].Headers),
                "IOutboxStorage.Get must return freshly-owned header dictionaries per call; a cached or stored dictionary instance is cleared by dispatch pooling.");
        }
        Assert.That(secondGet.TransportOperations[0].Headers, Is.SupersetOf(new Dictionary<string, string>
        {
            { "HeaderPooling.Key1", "value1" },
            { "HeaderPooling.Key2", "value2" },
        }), "IOutboxStorage.Get must return freshly-owned header dictionaries; shared references to stored state are cleared by dispatch pooling and corrupt the stored outbox entry.");

        // Prove the dedup lifecycle still completes on the (allegedly) uncorrupted entry.
        await storage.SetAsDispatched(messageId, configuration.GetContextBagForOutbox());
    }

    PersistenceTestsConfiguration configuration;
    readonly TestVariant param = param.DeepCopy();
}