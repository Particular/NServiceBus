namespace NServiceBus.Core.Tests.Pipeline
{
    using System.Threading.Tasks;
    using System.Transactions;
    using NUnit.Framework;

    [TestFixture]
    public class HandlerTransactionScopeWrapperBehaviorTests
    {
        [Test]
        public async Task ShouldNotInterfereWithExistingScope()
        {
            var behavior = new HandlerTransactionScopeWrapperBehavior(new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted });

            using (new TransactionScope(TransactionScopeOption.Required, new TransactionOptions
            {
                IsolationLevel = IsolationLevel.Serializable
            }, TransactionScopeAsyncFlowOption.Enabled))
            {
                await behavior.Invoke(null, () => Task.FromResult(0));
            }
        }

        [Test]
        public async Task ShouldWrapInnerBehaviorsIfNoAmbientExists()
        {
            var behavior = new HandlerTransactionScopeWrapperBehavior(new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted });

            await behavior.Invoke(null, () =>
            {
                Assert.NotNull(Transaction.Current);
                return Task.FromResult(0);
            });
        }
    }
}