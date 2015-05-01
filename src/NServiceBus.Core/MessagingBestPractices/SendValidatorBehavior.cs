namespace NServiceBus
{
    using System;
    using NServiceBus.MessagingBestPractices;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast;

    class SendValidatorBehavior : Behavior<OutgoingContext>
    {
        readonly Validations validations;

        public SendValidatorBehavior(Validations validations)
        {
            this.validations = validations;
        }

        public override void Invoke(OutgoingContext context, Action next)
        {
            //note: this check doesn't belong here. Will be moved in a different pull
            ValidateDestination(context);

            VerifyBestPractices(context.MessageType, context.Intent);
            
            next();
        }

        void VerifyBestPractices(Type messageType, MessageIntentEnum intent)
        {

            switch (intent)
            {
                case MessageIntentEnum.Publish:
                    validations.AssertIsValidForPubSub(messageType);
                    break;
                case MessageIntentEnum.Send:
                    validations.AssertIsValidForSend(messageType);
                    break;
                case MessageIntentEnum.Reply:
                    validations.AssertIsValidForReply(messageType);
                    break;
            }
        }

        void ValidateDestination(OutgoingContext context)
        {
            var sendOptions = context.DeliveryMessageOptions as SendMessageOptions;

            if (sendOptions != null && string.IsNullOrWhiteSpace(sendOptions.Destination))
            {
                throw new InvalidOperationException("No destination specified for message: " + context.MessageType);
            }
        }
    }
}