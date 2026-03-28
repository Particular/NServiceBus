namespace NServiceBus.Persistence.InMemory;

using System.Collections.Generic;
using NUnit.Framework;

[TestFixture]
public class When_committing_storage_transaction_successfully
{
    [Test]
    public void Should_apply_state_based_operations_in_order()
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

        Assert.That(committedValues, Is.EqualTo(expectedCommittedValues));
    }

    readonly record struct TransactionState(List<string> Values, string Value);

    static readonly string[] expectedCommittedValues = ["first", "second"];
}