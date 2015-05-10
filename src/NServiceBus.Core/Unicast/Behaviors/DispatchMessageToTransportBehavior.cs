namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;
    using NServiceBus.Unicast.Queuing;

    class DispatchMessageToTransportBehavior : PhysicalOutgoingContextStageBehavior
    {
        public ISendMessages MessageSender { get; set; }

        public IPublishMessages MessagePublisher { get; set; }

        public IDeferMessages MessageDeferral { get; set; }

        public override void Invoke(Context context, Action next)
        {
            InvokeNative(context);

            next();
        }

        public void InvokeNative(Context context)
        {
            context.Headers[Headers.MessageIntent] = context.Intent.ToString();

            var message = new OutgoingMessage(context.MessageId, context.Headers, context.Body);



            if (context.Intent == MessageIntentEnum.Publish)
            {
                NativePublish(new TransportPublishOptions(context.MessageType, context.DeliveryMessageOptions.TimeToBeReceived, context.DeliveryMessageOptions.NonDurable ?? false), message);
            }
            else
            {
                NativeSendOrDefer(context.DeliveryMessageOptions, message);
            }
        }

        public void NativePublish(TransportPublishOptions publishOptions, OutgoingMessage message)
        {
            SetTransportHeaders(publishOptions.TimeToBeReceived, publishOptions.NonDurable, message);

            try
            {
                Publish(message, publishOptions);
            }
            catch (QueueNotFoundException ex)
            {
                var messageDescription = "ControlMessage";

                string enclosedMessageTypes;

                if (message.Headers.TryGetValue(Headers.EnclosedMessageTypes, out enclosedMessageTypes))
                {
                    messageDescription = enclosedMessageTypes;
                }
                throw new Exception(string.Format("The destination queue '{0}' could not be found. You may have misconfigured the destination for this kind of message ({1}) in the MessageEndpointMappings of the UnicastBusConfig section in your configuration file. " + "It may also be the case that the given queue just hasn't been created yet, or has been deleted.", ex.Queue, messageDescription), ex);
            }
        }

        public void NativeSendOrDefer(DeliveryMessageOptions deliveryMessageOptions, OutgoingMessage message)
        {
            SetTransportHeaders(deliveryMessageOptions.TimeToBeReceived, deliveryMessageOptions.NonDurable, message);

            try
            {
                SendOrDefer(message, deliveryMessageOptions as SendMessageOptions);
            }
            catch (QueueNotFoundException ex)
            {
                var messageDescription = "ControlMessage";

                string enclosedMessageTypes;

                if (message.Headers.TryGetValue(Headers.EnclosedMessageTypes, out enclosedMessageTypes))
                {
                    messageDescription = enclosedMessageTypes;
                }
                throw new Exception(string.Format("The destination queue '{0}' could not be found. You may have misconfigured the destination for this kind of message ({1}) in the MessageEndpointMappings of the UnicastBusConfig section in your configuration file. " + "It may also be the case that the given queue just hasn't been created yet, or has been deleted.", ex.Queue, messageDescription), ex);
            }
        }

        void SetTransportHeaders(TimeSpan? timeToBeReceived, bool? nonDurable, OutgoingMessage message)
        {
            message.Headers[Headers.MessageId] = message.MessageId;


            if (timeToBeReceived.HasValue)
            {
                message.Headers[Headers.TimeToBeReceived] = timeToBeReceived.Value.ToString("c");
            }

            if (nonDurable.HasValue && nonDurable.Value)
            {
                message.Headers[Headers.NonDurableMessage] = true.ToString();
            }
        }

        void SendOrDefer(OutgoingMessage message, SendMessageOptions options)
        {
            if ((options.DelayDeliveryFor.HasValue && options.DelayDeliveryFor > TimeSpan.Zero) ||
                (options.DeliverAt.HasValue && options.DeliverAt.Value.ToUniversalTime() > DateTime.UtcNow))
            {
                SetIsDeferredHeader(message.Headers);
                MessageDeferral.Defer(message, new TransportDeferOptions(
                    options.Destination,
                    options.DelayDeliveryFor,
                    options.DeliverAt,
                    options.NonDurable ?? true,
                    options.EnlistInReceiveTransaction));

                return;
            }

            MessageSender.Send(message, new TransportSendOptions(options.Destination,
                                                                    options.TimeToBeReceived,
                                                                    options.NonDurable ?? true,
                                                                    options.EnlistInReceiveTransaction));
        }

        static void SetIsDeferredHeader(Dictionary<string, string> headers)
        {
            headers[Headers.IsDeferredMessage] = true.ToString();
        }

        void Publish(OutgoingMessage message, TransportPublishOptions publishOptions)
        {
            if (MessagePublisher == null)
            {
                throw new InvalidOperationException("No message publisher has been registered. If you're using a transport without native support for pub/sub please enable the message driven publishing feature by calling config.EnableFeature<MessageDrivenSubscriptions>() in your configuration");
            }
            MessagePublisher.Publish(message, publishOptions);
        }


    }
}