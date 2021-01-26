﻿namespace NServiceBus.Core.Tests.Pipeline
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using NUnit.Framework;

    [TestFixture]
    public class HandlerTransactionScopeWrapperBehaviorTests
    {
        [Test]
        public void ShouldBlowUpIfExistingScopeExists()
        {
            var behavior = new TransactionScopeUnitOfWorkBehavior(new TransactionOptions());

            Assert.That(async () =>
            {
                using (new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
                {
                    await behavior.Invoke(null, (_, __) => Task.CompletedTask, default);
                }
            }, Throws.InstanceOf<Exception>().And.Message.Contains("Ambient transaction detected. The transaction scope unit of work is not supported when there already is a scope present."));
        }

        [Test]
        public Task ShouldWrapInnerBehaviorsIfNoAmbientExists()
        {
            var behavior = new TransactionScopeUnitOfWorkBehavior(new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted });

            return behavior.Invoke(null, (_, __) =>
            {
                Assert.NotNull(Transaction.Current);
                return Task.CompletedTask;
            }, default);
        }
    }
}