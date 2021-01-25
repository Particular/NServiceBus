namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class EnforceSubscribeBestPracticesBehavior : IBehavior<ISubscribeContext, ISubscribeContext>
    {
        public EnforceSubscribeBestPracticesBehavior(Validations validations)
        {
            this.validations = validations;
        }

        public Task Invoke(ISubscribeContext context, Func<ISubscribeContext, Task> next)
        {
            if (!context.Extensions.TryGet(out EnforceBestPracticesOptions options) || options.Enabled)
            {
                foreach (var eventType in context.EventTypes)
                {
                    validations.AssertIsValidForPubSub(eventType);
                }
            }

            return next(context);
        }

        readonly Validations validations;
    }
}