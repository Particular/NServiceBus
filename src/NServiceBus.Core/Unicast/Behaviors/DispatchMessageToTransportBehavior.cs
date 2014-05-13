namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using Logging;
    using Messages;
    using Pipeline;
    using Pipeline.Contexts;
    using Queuing;
    using Transports;

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class DispatchMessageToTransportBehavior : IBehavior<OutgoingContext>
    {
        public ISendMessages MessageSender { get; set; }

        public IPublishMessages MessagePublisher { get; set; }

        public IDeferMessages MessageDeferral { get; set; }


        public void Invoke(OutgoingContext context, Action next)
        {
            InvokeNative(context.SendOptions, context.OutgoingMessage, context.OutgoingLogicalMessage.Metadata);

            next();
        }

        public void InvokeNative(SendOptions sendOptions, TransportMessage messageToSend, MessageMetadata metadata)
        {
            var messageDescription = "ControlMessage";

            string enclosedMessageTypes;

            if (messageToSend.Headers.TryGetValue(Headers.EnclosedMessageTypes, out enclosedMessageTypes))
            {
                messageDescription = enclosedMessageTypes;
            }

            try
            {
                if (sendOptions.Intent == MessageIntentEnum.Publish)
                {
                    Publish(messageToSend, metadata);
                }
                else
                {
                    SendOrDefer(messageToSend, sendOptions);
                }
            }
            catch (QueueNotFoundException ex)
            {
                throw new Exception(string.Format("The destination queue '{0}' could not be found. You may have misconfigured the destination for this kind of message ({1}) in the MessageEndpointMappings of the UnicastBusConfig section in your configuration file. " + "It may also be the case that the given queue just hasn't been created yet, or has been deleted.", ex.Queue, messageDescription), ex);
            }

            if (Log.IsDebugEnabled)
            {
                Log.DebugFormat("Sending message {0} with ID {1} to destination {2}.\n" + "Message headers:\n{3}", messageDescription,
                    messageToSend.Id,
                    sendOptions.Destination,
                    string.Join(", ", messageToSend.Headers.Select(h => h.Key + ":" + h.Value).ToArray()));
            }
        }

        void SendOrDefer(TransportMessage messageToSend, SendOptions sendOptions)
        {
            if (sendOptions.DelayDeliveryWith.HasValue)
            {
                if (sendOptions.DelayDeliveryWith > TimeSpan.Zero)
                {
                    SetIsDeferredHeader(messageToSend);
                    MessageDeferral.Defer(messageToSend, sendOptions.DelayDeliveryWith.Value, sendOptions.Destination);
                }
                else
                {
                    MessageSender.Send(messageToSend, sendOptions.Destination);
                }
                return;
            }

            if (sendOptions.DeliverAt.HasValue)
            {
                var deliverAt = sendOptions.DeliverAt.Value.ToUniversalTime();
                if (deliverAt > DateTime.UtcNow)
                {
                    SetIsDeferredHeader(messageToSend);
                    MessageDeferral.Defer(messageToSend, deliverAt, sendOptions.Destination);
                }
                else
                {
                    MessageSender.Send(messageToSend, sendOptions.Destination);
                }
                return;
            }
            MessageSender.Send(messageToSend, sendOptions.Destination);
        }

        static void SetIsDeferredHeader(TransportMessage messageToSend)
        {
            messageToSend.Headers[Headers.IsDeferredMessage] = true.ToString();
        }

        void Publish(TransportMessage messageToSend, MessageMetadata metadata)
        {
            if (MessagePublisher == null)
            {
                throw new InvalidOperationException("No message publisher has been registered. If you're using a transport without native support for pub/sub please enable the message driven publishing feature by calling: Feature.Enable<MessageDrivenPublisher>() in your configuration");
            }

            var eventTypesToPublish = metadata.MessageHierarchy
                .Distinct()
                .ToList();

            MessagePublisher.Publish(messageToSend, eventTypesToPublish);
        }

        static ILog Log = LogManager.GetLogger(typeof(DispatchMessageToTransportBehavior));
    }
}