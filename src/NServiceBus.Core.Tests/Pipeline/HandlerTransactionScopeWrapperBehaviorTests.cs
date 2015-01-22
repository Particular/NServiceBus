namespace NServiceBus.Core.Tests.Pipeline
{
    using System.Transactions;
    using NUnit.Framework;

    [TestFixture]
    public class HandlerTransactionScopeWrapperBehaviorTests
    {
        [Test]
        public void ShouldNotInterfereWithExistingScope()
        {
            var behavior = new HandlerTransactionScopeWrapperBehavior(new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted });

            using (new TransactionScope(TransactionScopeOption.Required, new TransactionOptions
            {
                IsolationLevel = IsolationLevel.Serializable
            }))
            {
                behavior.Invoke(null, () => { });
            }
        }

        [Test]
        public void ShouldWrapInnerBehaviorsIfNoAmbientExists()
        {
            var behavior = new HandlerTransactionScopeWrapperBehavior(new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted });

            behavior.Invoke(null, () => Assert.NotNull(Transaction.Current));

        }
    }
}