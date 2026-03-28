namespace NServiceBus.Persistence.InMemory;

using System;
using System.Collections.Generic;
using NUnit.Framework;

[TestFixture]
public class When_committing_storage_transaction_with_failure
{
    [Test]
    public void Should_rollback_already_applied_operations_when_a_later_operation_fails()
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

    readonly record struct TransactionState(List<string> Values, string Value);

    readonly record struct ThrowingTransactionState(Exception Exception);
}