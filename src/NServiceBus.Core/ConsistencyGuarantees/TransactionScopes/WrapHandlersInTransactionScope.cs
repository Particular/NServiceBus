namespace NServiceBus.Features
{
    using System;
    using System.Transactions;

    class WrapHandlersInTransactionScope : Feature
    {
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = context.Settings.Get<IsolationLevel>("Transactions.IsolationLevel"),
                Timeout = context.Settings.Get<TimeSpan>("Transactions.DefaultTimeout")
            };

            context.Container.ConfigureComponent(b => new HandlerTransactionScopeWrapperBehavior(transactionOptions), DependencyLifecycle.InstancePerCall);

            context.Pipeline.Register("HandlerTransactionScopeWrapper", typeof(HandlerTransactionScopeWrapperBehavior), "Makes sure that the handlers gets wrapped in a transaction scope");
        }
    }
}
