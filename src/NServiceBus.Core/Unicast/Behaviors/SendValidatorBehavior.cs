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
            if (!context.SendOptions.EnforceMessagingBestPractices)
            {
                return;
            }
            if (context.SendOptions.Destination == Address.Undefined)
            {
                throw new InvalidOperationException("No destination specified for message: " + context.OutgoingLogicalMessage.MessageType);
            }

            switch (context.SendOptions.Intent)
            {
                case MessageIntentEnum.Subscribe:
                case MessageIntentEnum.Unsubscribe:
                    break;
                case MessageIntentEnum.Publish:
                    MessagingBestPractices.AssertIsValidForPubSub(context.OutgoingLogicalMessage.MessageType);
                    break;
                case MessageIntentEnum.Reply:
                    MessagingBestPractices.AssertIsValidForReply(context.OutgoingLogicalMessage.MessageType);
                    break;
                case MessageIntentEnum.Send:
                    MessagingBestPractices.AssertIsValidForSend(context.OutgoingLogicalMessage.MessageType, context.SendOptions.Intent);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}