namespace NServiceBus
{
    using System;
    using NServiceBus.MessagingBestPractices;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Routing.MessagingBestPractices;

    class EnforceSubscribeBestPracticesBehavior : Behavior<SubscribeContext>
    {
        public EnforceSubscribeBestPracticesBehavior(Validations validations)
        {
            this.validations = validations;
        }

        public override void Invoke(SubscribeContext context, Action next)
        {
            EnforceBestPracticesOptions options;

            if (!context.TryGet(out options) || options.Enabled)
            {
                validations.AssertIsValidForPubSub(context.EventType);
            }

            next();
        }

        Validations validations;
    }
}