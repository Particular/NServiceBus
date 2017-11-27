namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class EnforceUnsubscribeBestPracticesBehavior : IBehavior<IUnsubscribeContext, IUnsubscribeContext>
    {
        public EnforceUnsubscribeBestPracticesBehavior(Validations validations)
        {
            this.validations = validations;
        }

        public Task Invoke(IUnsubscribeContext context, Func<IUnsubscribeContext, Task> next)
        {
            if (!context.Extensions.TryGet(out EnforceBestPracticesOptions options) || options.Enabled)
            {
                validations.AssertIsValidForPubSub(context.EventType);
            }

            return next(context);
        }

        readonly Validations validations;
    }
}