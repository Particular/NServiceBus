namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.Pipeline;

    class SuppressAmbientTransactionBehavior : PhysicalMessageProcessingStageBehavior
    {
        public override async Task Invoke(Context context, Func<Task> next)
        {
            if (Transaction.Current == null)
            {
                await next().ConfigureAwait(false);
                return;
            }

            using (var tx = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
            {
                await next().ConfigureAwait(false);

                tx.Complete();
            }
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("HandlerTransactionScopeWrapperBehavior", typeof(SuppressAmbientTransactionBehavior), "Make sure that any ambient transaction scope is suppressed")
            {
                InsertBeforeIfExists("FirstLevelRetries");
                InsertBeforeIfExists("ReceivePerformanceDiagnosticsBehavior");
            }
        }
    }
}