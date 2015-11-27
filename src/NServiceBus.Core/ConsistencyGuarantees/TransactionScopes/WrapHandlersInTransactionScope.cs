namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Transactions;

    class WrapHandlersInTransactionScope : Feature
    {
        public WrapHandlersInTransactionScope()
        {
            EnableByDefault();
        }

        protected internal override IReadOnlyCollection<FeatureStartupTask> Setup(FeatureConfigurationContext context)
        {
            if (context.Settings.GetOrDefault<bool>("Transactions.DoNotWrapHandlersExecutionInATransactionScope"))
            {
                context.Pipeline.Register("HandlerTransactionScopeWrapperBehavior", typeof(SuppressAmbientTransactionBehavior), "Make sure that any ambient transaction scope is suppressed");
            }
            else
            {
                var transactionOptions = new TransactionOptions
                {
                    IsolationLevel = context.Settings.Get<IsolationLevel>("Transactions.IsolationLevel"),
                    Timeout = context.Settings.Get<TimeSpan>("Transactions.DefaultTimeout")
                };

                context.Container.ConfigureComponent(b => new HandlerTransactionScopeWrapperBehavior(transactionOptions), DependencyLifecycle.InstancePerCall);

                context.Pipeline.Register("HandlerTransactionScopeWrapper", typeof(HandlerTransactionScopeWrapperBehavior), "Makes sure that the handlers gets wrapped in a transaction scope");
            }

            return FeatureStartupTask.None;
        }
    }
}
