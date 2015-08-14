namespace NServiceBus
{
    using System;
    using NServiceBus.MessagingBestPractices;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Routing.MessagingBestPractices;

    class EnforceUnsubscribeBestPracticesBehavior : Behavior<UnsubscribeContext>
    {
        public EnforceUnsubscribeBestPracticesBehavior(Validations validations)
        {
            this.validations = validations;
        }

        public override void Invoke(UnsubscribeContext context, Action next)
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