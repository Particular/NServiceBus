namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class HandlerTransactionScopeWrapperBehavior : HandlingStageBehavior
    {
        TransactionOptions transactionOptions;

        public HandlerTransactionScopeWrapperBehavior(TransactionOptions transactionOptions)
        {
            this.transactionOptions = transactionOptions;
        }

        public override async Task Invoke(Context context, Func<Task> next)
        {
            if (Transaction.Current != null)
            {
                await next().ConfigureAwait(false);
                return;
            }

            using (var tx = new TransactionScope(TransactionScopeOption.Required, transactionOptions, TransactionScopeAsyncFlowOption.Enabled))
            {
                await next().ConfigureAwait(false);

                tx.Complete();
            }
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("HandlerTransactionScopeWrapper", typeof(HandlerTransactionScopeWrapperBehavior), "Makes sure that the handlers gets wrapped in a transaction scope")
            {
                InsertBeforeIfExists(WellKnownStep.InvokeHandlers);

                ContainerRegistration((builder, settings) =>
                {
                    var transactionOptions = new TransactionOptions
                    {
                        IsolationLevel = settings.Get<IsolationLevel>("Transactions.IsolationLevel"),
                        Timeout = settings.Get<TimeSpan>("Transactions.DefaultTimeout")
                    };


                    return new HandlerTransactionScopeWrapperBehavior(transactionOptions);
                });
            }
        }
    }
}