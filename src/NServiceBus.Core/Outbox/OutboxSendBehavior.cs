namespace NServiceBus
{
    using System;
    using NServiceBus.Outbox;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast;

    class OutboxSendBehavior : PhysicalOutgoingContextStageBehavior
    {
        readonly DispatchMessageToTransportBehavior dispatchMessageToTransportBehavior;

        public OutboxSendBehavior(DispatchMessageToTransportBehavior dispatchMessageToTransportBehavior)
        {
            this.dispatchMessageToTransportBehavior = dispatchMessageToTransportBehavior;
        }

        public override void Invoke(Context context, Action next)
        {
            OutboxMessage currentOutboxMessage;

            if (context.TryGet(out currentOutboxMessage))
            {
                var options = context.DeliveryMessageOptions.ToTransportOperationOptions();

                if (context.Intent == MessageIntentEnum.Publish)
                {
                    options["Operation"] = "Publish";
                    options["EventType"] = context.MessageType.AssemblyQualifiedName;
                }
                else
                {
                    var sendOptions = (SendMessageOptions)context.DeliveryMessageOptions;
         
                    options["Operation"] = "Send";
                    options["Destination"] = sendOptions.Destination;
          
                    if (sendOptions.DelayDeliveryFor.HasValue)
                    {
                        options["DelayDeliveryFor"] = sendOptions.DelayDeliveryFor.Value.ToString();
                    }

                    if (sendOptions.DeliverAt.HasValue)
                    {
                        options["DeliverAt"] = DateTimeExtensions.ToWireFormattedString(sendOptions.DeliverAt.Value);
                    }

                }

                var messageToSend = dispatchMessageToTransportBehavior.GetOutgoingMessage(context);

                currentOutboxMessage.TransportOperations.Add(new TransportOperation(messageToSend.MessageId, options, messageToSend.Body, messageToSend.Headers)); 
            }
            else
            {
                dispatchMessageToTransportBehavior.InvokeNative(context);

                next();
            }
        }
    }
}