namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.ComponentModel;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast;

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class SendValidatorBehavior : IBehavior<SendLogicalMessageContext>
    {
        public void Invoke(SendLogicalMessageContext context, Action next)
        {
            VerifyBestPractices(context);

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
                throw new InvalidOperationException("No destination specified for message: " + context.MessageToSend.MessageType);
            }

            switch (context.SendOptions.Intent)
            {
                case MessageIntentEnum.Init:
                case MessageIntentEnum.Subscribe:
                case MessageIntentEnum.Unsubscribe:
                    break;
                case MessageIntentEnum.Publish:
                    MessagingBestPractices.AssertIsValidForPubSub(context.MessageToSend.MessageType);
                    break;
                case MessageIntentEnum.Reply:
                    MessagingBestPractices.AssertIsValidForReply(context.MessageToSend.MessageType);
                    break;
                case MessageIntentEnum.Send:
                    MessagingBestPractices.AssertIsValidForSend(context.MessageToSend.MessageType, context.SendOptions.Intent);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}