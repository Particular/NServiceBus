namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast;

    class SendValidatorBehavior : Behavior<OutgoingContext>
    {
        public Conventions Conventions { get; set; }

        public override void Invoke(OutgoingContext context, Action next)
        {
            if (!context.IsControlMessage)
            {
                VerifyBestPractices(context);
            }

            next();
        }

        void VerifyBestPractices(OutgoingContext context)
        {
            if (!context.IsControlMessage)
            {
                return;
            }

            if (!context.DeliveryMessageOptions.EnforceMessagingBestPractices)
            {
                return;
            }

            var sendOptions = context.DeliveryMessageOptions as SendMessageOptions;

            if (sendOptions == null)
            {
                MessagingBestPractices.AssertIsValidForPubSub(context.MessageType, Conventions);
                return;
            }

            if (string.IsNullOrWhiteSpace(sendOptions.Destination))
            {
                throw new InvalidOperationException("No destination specified for message: " + context.MessageType);
            }

            if (context.Intent == MessageIntentEnum.Reply)
            {
                MessagingBestPractices.AssertIsValidForReply(context.MessageType, Conventions);
            }
            else
            {
                MessagingBestPractices.AssertIsValidForSend(context.MessageType, Conventions);
            }
        }
    }
}