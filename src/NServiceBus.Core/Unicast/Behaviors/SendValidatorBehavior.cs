namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.ComponentModel;
    using Pipeline;
    using Pipeline.Contexts;
    using Transport;
    using Unicast;

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class SendValidatorBehavior : IBehavior<SendLogicalMessageContext>
    {
        public void Invoke(SendLogicalMessageContext context, Action next)
        {
            if (!context.LogicalMessage.IsControlMessage())
            {
                VerifyBestPractices(context);
            }

            next();
        }

        static void VerifyBestPractices(SendLogicalMessageContext context)
        {
            if (!context.SendOptions.EnforceMessagingBestPractices)
            {
                return;
            }
            if (context.SendOptions.Destination == Address.Undefined)
            {
                throw new InvalidOperationException("No destination specified for message: " + context.LogicalMessage.MessageType);
            }

            switch (context.SendOptions.Intent)
            {
                case MessageIntentEnum.Subscribe:
                case MessageIntentEnum.Unsubscribe:
                    break;
                case MessageIntentEnum.Publish:
                    MessagingBestPractices.AssertIsValidForPubSub(context.LogicalMessage.MessageType);
                    break;
                case MessageIntentEnum.Reply:
                    MessagingBestPractices.AssertIsValidForReply(context.LogicalMessage.MessageType);
                    break;
                case MessageIntentEnum.Send:
                    MessagingBestPractices.AssertIsValidForSend(context.LogicalMessage.MessageType, context.SendOptions.Intent);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}