namespace NServiceBus
{
    using System;
    using System.Transactions;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class HandlerTransactionScopeWrapperBehavior : HandlingStageBehavior
    {
        readonly TransactionOptions transactionOptions;

        public HandlerTransactionScopeWrapperBehavior(TransactionOptions transactionOptions)
        {
            this.transactionOptions = transactionOptions;
        }

        public override void Invoke(Context context, Action next)
        {
            if (Transaction.Current != null)
            {
                next();
                return;
            }

            using (var tx = new TransactionScope(TransactionScopeOption.Required, transactionOptions))
            {
                next();

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