namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Hosting;
    using NServiceBus.Unicast;
    using NServiceBus.Unicast.Queuing;
    using Settings;
    using Pipeline.Contexts;
    using Support;
    using Transports;

    class DispatchMessageToTransportBehavior : PhysicalOutgoingContextStageBehavior
    {
        public ISendMessages MessageSender { get; set; }

        public IPublishMessages MessagePublisher { get; set; }

        public IDeferMessages MessageDeferral { get; set; }

        public ReadOnlySettings Settings { get; set; }

        public HostInformation HostInfo { get; set; }

        public override void Invoke(Context context, Action next)
        {
            InvokeNative(context.DeliveryMessageOptions, new OutgoingMessage(context.MessageId,context.Headers,context.Body));

            next();
        }

        public void InvokeNative(DeliveryMessageOptions deliveryMessageOptions, OutgoingMessage message)
        {
            var messageDescription = "ControlMessage";

            string enclosedMessageTypes;

            if (message.Headers.TryGetValue(Headers.EnclosedMessageTypes, out enclosedMessageTypes))
            {
                messageDescription = enclosedMessageTypes;
            }

            message.Headers.Add(Headers.OriginatingMachine, RuntimeEnvironment.MachineName);
            message.Headers.Add(Headers.OriginatingEndpoint, Settings.EndpointName());
            message.Headers.Add(Headers.OriginatingHostId, HostInfo.HostId.ToString("N"));
            message.Headers[Headers.MessageId] = message.MessageId;


            if (deliveryMessageOptions.TimeToBeReceived.HasValue)
            {
                message.Headers[Headers.TimeToBeReceived] = deliveryMessageOptions.TimeToBeReceived.Value.ToString("c");   
            }

            if (deliveryMessageOptions.NonDurable.HasValue && deliveryMessageOptions.NonDurable.Value)
            {
                message.Headers[Headers.NonDurableMessage] = true.ToString();
            }

          
            try
            {
                var publishOptions = deliveryMessageOptions as PublishMessageOptions;
                if(publishOptions != null)
                {
                    Publish(message, publishOptions);
                }
                else
                {

                    SendOrDefer(message, (SendMessageOptions)deliveryMessageOptions);
                }
            }
            catch (QueueNotFoundException ex)
            {
                throw new Exception(string.Format("The destination queue '{0}' could not be found. You may have misconfigured the destination for this kind of message ({1}) in the MessageEndpointMappings of the UnicastBusConfig section in your configuration file. " + "It may also be the case that the given queue just hasn't been created yet, or has been deleted.", ex.Queue, messageDescription), ex);
            }
        }

        void SendOrDefer(OutgoingMessage message, SendMessageOptions options)
        {
            var sendOptions = new TransportSendOptions(options.Destination, options.TimeToBeReceived, options.NonDurable ?? true, options.EnlistInReceiveTransaction);

            if (options.DelayDeliveryFor.HasValue)
            {
                if (options.DelayDeliveryFor > TimeSpan.Zero)
                {
                    SetIsDeferredHeader(message.Headers);
                    MessageDeferral.Defer(message, options);
                }
                else
                {
                    MessageSender.Send(message, sendOptions);
                }

                return;
            }

            if (options.DeliverAt.HasValue)
            {
                var deliverAt = options.DeliverAt.Value.ToUniversalTime();
                if (deliverAt > DateTime.UtcNow)
                {
                    SetIsDeferredHeader(message.Headers);
                    MessageDeferral.Defer(message, options);
                }
                else
                {
                    MessageSender.Send(message, sendOptions);
                }

                return;
            }

            MessageSender.Send(message, sendOptions);
        }

        static void SetIsDeferredHeader(Dictionary<string,string> headers)
        {
            headers[Headers.IsDeferredMessage] = true.ToString();
        }

        void Publish(OutgoingMessage message, PublishMessageOptions publishOptions)
        {
            if (MessagePublisher == null)
            {
                throw new InvalidOperationException("No message publisher has been registered. If you're using a transport without native support for pub/sub please enable the message driven publishing feature by calling config.EnableFeature<MessageDrivenSubscriptions>() in your configuration");
            }
            MessagePublisher.Publish(message, publishOptions);
        }
    }
}