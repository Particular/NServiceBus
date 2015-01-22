namespace NServiceBus
{
    using System;
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
            InvokeNative(context.DeliveryOptions, context.OutgoingMessage);

            next();
        }

        public void InvokeNative(DeliveryOptions deliveryOptions, TransportMessage messageToSend)
        {
            var messageDescription = "ControlMessage";

            string enclosedMessageTypes;

            if (messageToSend.Headers.TryGetValue(Headers.EnclosedMessageTypes, out enclosedMessageTypes))
            {
                messageDescription = enclosedMessageTypes;
            }

            messageToSend.Headers.Add(Headers.OriginatingMachine, RuntimeEnvironment.MachineName);
            messageToSend.Headers.Add(Headers.OriginatingEndpoint, Settings.EndpointName());
            messageToSend.Headers.Add(Headers.OriginatingHostId, HostInfo.HostId.ToString("N"));
          
            try
            {
                if(deliveryOptions is PublishOptions)
                {
                    Publish(messageToSend, deliveryOptions as PublishOptions);
                }
                else
                {
                    SendOrDefer(messageToSend, deliveryOptions as SendOptions);
                }
            }
            catch (QueueNotFoundException ex)
            {
                throw new Exception(string.Format("The destination queue '{0}' could not be found. You may have misconfigured the destination for this kind of message ({1}) in the MessageEndpointMappings of the UnicastBusConfig section in your configuration file. " + "It may also be the case that the given queue just hasn't been created yet, or has been deleted.", ex.Queue, messageDescription), ex);
            }
        }

        void SendOrDefer(TransportMessage messageToSend, SendOptions sendOptions)
        {
            if (sendOptions.DelayDeliveryWith.HasValue)
            {
                if (sendOptions.DelayDeliveryWith > TimeSpan.Zero)
                {
                    SetIsDeferredHeader(messageToSend);
                    MessageDeferral.Defer(messageToSend, sendOptions);
                }
                else
                {
                    MessageSender.Send(messageToSend, sendOptions);
                }

                return;
            }

            if (sendOptions.DeliverAt.HasValue)
            {
                var deliverAt = sendOptions.DeliverAt.Value.ToUniversalTime();
                if (deliverAt > DateTime.UtcNow)
                {
                    SetIsDeferredHeader(messageToSend);
                    MessageDeferral.Defer(messageToSend, sendOptions);
                }
                else
                {
                    MessageSender.Send(messageToSend, sendOptions);
                }

                return;
            }

            MessageSender.Send(messageToSend, sendOptions);
        }

        static void SetIsDeferredHeader(TransportMessage messageToSend)
        {
            messageToSend.Headers[Headers.IsDeferredMessage] = true.ToString();
        }

        void Publish(TransportMessage messageToSend,PublishOptions publishOptions)
        {
            if (MessagePublisher == null)
            {
                throw new InvalidOperationException("No message publisher has been registered. If you're using a transport without native support for pub/sub please enable the message driven publishing feature by calling config.EnableFeature<MessageDrivenSubscriptions>() in your configuration");
            }

            MessagePublisher.Publish(messageToSend, publishOptions);
        }
    }
}