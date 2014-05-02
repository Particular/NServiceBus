namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using Logging;
    using Pipeline;
    using Pipeline.Contexts;
    using Transports;
    using Queuing;

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class DispatchMessageToTransportBehavior : IBehavior<SendPhysicalMessageContext>
    {
        public ISendMessages MessageSender { get; set; }

        public IPublishMessages MessagePublisher { get; set; }

        public IDeferMessages MessageDeferral { get; set; }


        public void Invoke(SendPhysicalMessageContext context, Action next)
        {
            var sendOptions = context.SendOptions;

            var logicalMessage = context.LogicalMessages.FirstOrDefault();
            var messageDescription = "ControlMessage";

            if (logicalMessage != null)
            {
                messageDescription = logicalMessage.MessageType.FullName;
            }

            try
            {
                InnerSend(context, sendOptions);
            }
            catch (QueueNotFoundException ex)
            {
                var message = string.Format("The destination queue '{0}' could not be found. You may have misconfigured the destination for this kind of message ({1}) in the MessageEndpointMappings of the UnicastBusConfig section in your configuration file. " + "It may also be the case that the given queue just hasn't been created yet, or has been deleted.", ex.Queue, messageDescription);
                throw new Exception(message, ex);
            }

            if (Log.IsDebugEnabled)
            {
                var message = string.Format("Sending message {0} with ID {1} to destination {2}.\n" +
                                               "ToString() of the message yields: {3}\n" +
                                               "Message headers:\n{4}",
                    messageDescription,
                    context.MessageToSend.Id,
                    sendOptions.Destination,
                    logicalMessage != null ? logicalMessage.ToString() : "",
                    string.Join(", ", context.MessageToSend.Headers.Select(h => h.Key + ":" + h.Value).ToArray())
                    );
                Log.Debug(message);
            }

            next();
        }

        void InnerSend(SendPhysicalMessageContext context, SendOptions sendOptions)
        {
            if (sendOptions.Intent == MessageIntentEnum.Publish)
            {
                Publish(context);
            }
            else
            {
                SendOrDefer(context, sendOptions);
            }
        }

        void SendOrDefer(SendPhysicalMessageContext context, SendOptions sendOptions)
        {
            if (sendOptions.DelayDeliveryWith.HasValue)
            {
                if (sendOptions.DelayDeliveryWith > TimeSpan.Zero)
                {
                    SetIsDeferredHeader(context);
                    MessageDeferral.Defer(context.MessageToSend, sendOptions.DelayDeliveryWith.Value, sendOptions.Destination);
                }
                else
                {
                    MessageSender.Send(context.MessageToSend, sendOptions.Destination);
                }
                return;
            }

            if (sendOptions.DeliverAt.HasValue)
            {
                var deliverAt = sendOptions.DeliverAt.Value.ToUniversalTime();
                if (deliverAt > DateTime.UtcNow)
                {
                    SetIsDeferredHeader(context);
                    MessageDeferral.Defer(context.MessageToSend, deliverAt, sendOptions.Destination);
                }
                else
                {
                    MessageSender.Send(context.MessageToSend, sendOptions.Destination);
                }
                return;
            }
            MessageSender.Send(context.MessageToSend, sendOptions.Destination);
        }

        static void SetIsDeferredHeader(SendPhysicalMessageContext context)
        {
            context.MessageToSend.Headers[Headers.IsDeferredMessage] = true.ToString();
        }

        void Publish(SendPhysicalMessageContext context)
        {
            if (MessagePublisher == null)
            {
                throw new InvalidOperationException("No message publisher has been registered. If you're using a transport without native support for pub/sub please enable the message driven publishing feature by calling: Feature.Enable<MessageDrivenPublisher>() in your configuration");
            }

            var eventTypesToPublish = context.LogicalMessages.SelectMany(m => m.Metadata.MessageHierarchy)
                .Distinct()
                .ToList();

            var subscribersFound = MessagePublisher.Publish(context.MessageToSend, eventTypesToPublish);

            context.Set("SubscribersFound", subscribersFound);
        }

        static ILog Log = LogManager.GetLogger(typeof(DispatchMessageToTransportBehavior));
    }
}