namespace NServiceBus.Persistence.InMemory;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Outbox;

[TestFixture]
public class InMemoryPersistenceDesignTests
{
    [Test]
    public async Task Dispatched_outbox_entries_should_expire_from_dispatch_time()
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

    [Test]
    public void Transaction_commit_should_rollback_already_applied_operations_when_a_later_operation_fails()
    {
        var transaction = new InMemoryStorageTransaction();
        var committedValues = new List<string>();

        transaction.Enlist(
            new TransactionState(committedValues, "first"),
            static state => state.Values.Add(state.Value),
            static state => state.Values.RemoveAt(state.Values.Count - 1));

        transaction.Enlist(
            new ThrowingTransactionState(new InvalidOperationException("boom")),
            static state => throw state.Exception,
            static _ => { });

        Assert.Throws<InvalidOperationException>(() => transaction.Commit());
        Assert.That(committedValues, Is.Empty);
    }

    [Test]
    public void Transaction_commit_should_apply_state_based_operations_in_order()
    {
        var transaction = new InMemoryStorageTransaction();
        var committedValues = new List<string>();

        transaction.Enlist(
            new TransactionState(committedValues, "first"),
            static state => state.Values.Add(state.Value),
            static state => state.Values.RemoveAt(state.Values.Count - 1));

        transaction.Enlist(
            new TransactionState(committedValues, "second"),
            static state => state.Values.Add(state.Value),
            static state => state.Values.RemoveAt(state.Values.Count - 1));

        transaction.Commit();

        Assert.That(committedValues, Is.EqualTo(ExpectedCommittedValues));
    }

    [Test]
    public void Storage_registration_should_use_dependency_injection_then_explicit_configuration_then_shared_default()
    {
        var serviceCollection = new ServiceCollection();
        var serviceProviderStorage = new InMemoryStorage();
        serviceCollection.AddSingleton(serviceProviderStorage);

        InMemoryStorageRuntime.Configure(serviceCollection, configuredStorage: new InMemoryStorage());

        using var provider = serviceCollection.BuildServiceProvider();

        Assert.That(provider.GetRequiredService<InMemoryStorage>(), Is.SameAs(serviceProviderStorage));

        var configuredServices = new ServiceCollection();
        var configuredStorage = new InMemoryStorage();

        InMemoryStorageRuntime.Configure(configuredServices, configuredStorage);

        using var configuredProvider = configuredServices.BuildServiceProvider();
        Assert.That(configuredProvider.GetRequiredService<InMemoryStorage>(), Is.SameAs(configuredStorage));

        var defaultServices = new ServiceCollection();

        InMemoryStorageRuntime.Configure(defaultServices, configuredStorage: null);

        using var defaultProvider = defaultServices.BuildServiceProvider();
        Assert.That(defaultProvider.GetRequiredService<InMemoryStorage>(), Is.SameAs(InMemoryStorageRuntime.SharedStorage));
    }

    readonly record struct TransactionState(List<string> Values, string Value);

    readonly record struct ThrowingTransactionState(Exception Exception);

    static readonly string[] ExpectedCommittedValues = ["first", "second"];
}
