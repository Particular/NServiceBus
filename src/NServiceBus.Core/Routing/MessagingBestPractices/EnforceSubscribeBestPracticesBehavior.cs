namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using MessagingBestPractices;
    using Pipeline;
    using Routing;
    using Routing.MessagingBestPractices;

    class EnforceSubscribeBestPracticesBehavior : Behavior<SubscribeContext>
    {
        public EnforceSubscribeBestPracticesBehavior(Validations validations)
        {
            this.validations = validations;
        }

        public override Task Invoke(SubscribeContext context, Func<Task> next)
        {
            EnforceBestPracticesOptions options;

            if (!context.TryGet(out options) || options.Enabled)
            {
                validations.AssertIsValidForPubSub(context.EventType);
            }

            return next();
        }

        Validations validations;
    }
}