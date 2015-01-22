namespace NServiceBus
{
    using System;
    using NServiceBus.Unicast.Transport;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast;

    class SendValidatorBehavior : Behavior<OutgoingContext>
    {
        public Conventions Conventions { get; set; }

        public override void Invoke(OutgoingContext context, Action next)
        {
            if (!context.OutgoingLogicalMessage.IsControlMessage())
            {
                VerifyBestPractices(context);
            }

            next();
        }

        void VerifyBestPractices(OutgoingContext context)
        {
            if (!context.DeliveryOptions.EnforceMessagingBestPractices)
            {
                return;
            }

            var sendOptions = context.DeliveryOptions as SendOptions;

            if (sendOptions == null)
            {
                MessagingBestPractices.AssertIsValidForPubSub(context.OutgoingLogicalMessage.MessageType, Conventions);
                return;
            }

            if (sendOptions.Destination == Address.Undefined)
            {
                throw new InvalidOperationException("No destination specified for message: " + context.OutgoingLogicalMessage.MessageType);
            }

            if (sendOptions is ReplyOptions)
            {
                MessagingBestPractices.AssertIsValidForReply(context.OutgoingLogicalMessage.MessageType, Conventions);
            }
            else
            {
                MessagingBestPractices.AssertIsValidForSend(context.OutgoingLogicalMessage.MessageType, Conventions);
            }
        }
    }
}