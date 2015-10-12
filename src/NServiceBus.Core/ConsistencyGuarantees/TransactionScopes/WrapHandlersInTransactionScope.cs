namespace NServiceBus.Features
{
    using System;
    using System.Transactions;

    class WrapHandlersInTransactionScope : Feature
    {
        public WrapHandlersInTransactionScope()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            bool doNotWrap;

            if (!context.Settings.TryGet("Transactions.DoNotWrapHandlersExecutionInATransactionScope", out doNotWrap))
            {
                return;
            }

            if (doNotWrap)
            {
                context.Pipeline.Register("SuppressAmbientTransaction", typeof(SuppressAmbientTransactionBehavior), "Make sure that any ambient transaction scope is suppressed");
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
        }
    }
}
