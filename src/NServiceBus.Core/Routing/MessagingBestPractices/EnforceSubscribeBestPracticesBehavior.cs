namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;

    class EnforceSubscribeBestPracticesBehavior : IBehavior<ISubscribeContext, ISubscribeContext>
    {
        public EnforceSubscribeBestPracticesBehavior(Validations validations)
        {
            this.validations = validations;
        }

        public Task Invoke(ISubscribeContext context, Func<ISubscribeContext, CancellationToken, Task> next, CancellationToken cancellationToken)
        {
            if (!context.Extensions.TryGet(out EnforceBestPracticesOptions options) || options.Enabled)
            {
                validations.AssertIsValidForPubSub(context.EventType);
            }

            return next(context, cancellationToken);
        }

        readonly Validations validations;
    }
}