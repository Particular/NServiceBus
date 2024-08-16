namespace NServiceBus.Core.Tests;

using System;
using System.Transactions;
using NUnit.Framework;

[TestFixture]
public class TransactionScopeTests
{
    [Test]
    public void Basic_assumptions_promotable_should_fail_if_durable_already_exists()
    {
        if (OperatingSystem.IsWindows())
        {
            TransactionManager.ImplicitDistributedTransactions = true;

            using (var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var resourceManager = new FakePromotableResourceManager();
                Transaction.Current.EnlistDurable(resourceManager.ResourceManagerId, new FakePromotableResourceManager(), EnlistmentOptions.None);
                Assert.That(Transaction.Current.EnlistPromotableSinglePhase(new FakePromotableResourceManager()), Is.False);

                tx.Complete();
            }
        }
        else
        {
            Assert.Ignore("Ignoring this test because it requires Windows");
        }
    }

    [Test]
    public void Basic_assumptions_second_promotable_should_fail()
    {
        using (var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            Assert.That(Transaction.Current.EnlistPromotableSinglePhase(new FakePromotableResourceManager()), Is.True);

            Assert.That(Transaction.Current.EnlistPromotableSinglePhase(new FakePromotableResourceManager()), Is.False);

            tx.Complete();
        }
    }

    public class FakePromotableResourceManager : IPromotableSinglePhaseNotification, IEnlistmentNotification
    {
        public void Prepare(PreparingEnlistment preparingEnlistment) => preparingEnlistment.Prepared();

        public void Commit(Enlistment enlistment) => enlistment.Done();

        public void Rollback(Enlistment enlistment) => enlistment.Done();

        public void InDoubt(Enlistment enlistment) => enlistment.Done();

        public void Initialize() { }

        public void SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment) => singlePhaseEnlistment.Committed();

        public void Rollback(SinglePhaseEnlistment singlePhaseEnlistment) => singlePhaseEnlistment.Done();

        public byte[] Promote() => TransactionInterop.GetTransmitterPropagationToken(new CommittableTransaction());

        public Guid ResourceManagerId = Guid.Parse("6f057e24-a0d8-4c95-b091-b8dc9a916fa4");
    }
}
