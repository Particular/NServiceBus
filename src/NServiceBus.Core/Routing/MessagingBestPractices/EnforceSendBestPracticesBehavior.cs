namespace NServiceBus
{
    using System;
    using NServiceBus.MessagingBestPractices;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing.MessagingBestPractices;

    class EnforceSendBestPracticesBehavior : Behavior<OutgoingSendContext>
    {
        Validations validations;

        public EnforceSendBestPracticesBehavior(Validations validations)
        {
            this.validations = validations;
        }

        public override void Invoke(OutgoingSendContext context, Action next)
        {
            EnforceBestPracticesOptions options;

            if (!context.TryGet(out options) || options.Enabled)
            {
                validations.AssertIsValidForSend(context.GetMessageType());
            }

            next();
        }
    }
}