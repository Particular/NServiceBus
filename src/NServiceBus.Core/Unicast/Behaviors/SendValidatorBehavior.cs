namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;
    using Transport;
    using Unicast;

    class SendValidatorBehavior : IBehavior<OutgoingContext>
    {
        public void Invoke(OutgoingContext context, Action next)
        {
            if (!context.OutgoingLogicalMessage.IsControlMessage())
            {
                VerifyBestPractices(context);
            }

            next();
        }

        static void VerifyBestPractices(OutgoingContext context)
        {
            if (!context.DeliveryOptions.EnforceMessagingBestPractices)
            {
                return;
            }

            var sendOptions = context.DeliveryOptions as SendOptions;

            if (sendOptions == null)
            {
                MessagingBestPractices.AssertIsValidForPubSub(context.OutgoingLogicalMessage.MessageType);
                return;
            }

            if (sendOptions.Destination == Address.Undefined)
            {
                throw new InvalidOperationException("No destination specified for message: " + context.OutgoingLogicalMessage.MessageType);
            }

            if (sendOptions is ReplyOptions)
            {
                MessagingBestPractices.AssertIsValidForReply(context.OutgoingLogicalMessage.MessageType);
            }
            else
            {
                MessagingBestPractices.AssertIsValidForSend(context.OutgoingLogicalMessage.MessageType);
            }
        }
    }
}