﻿namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.ComponentModel;
    using System.Configuration;
    using System.Linq;
    using Logging;
    using Pipeline;
    using Pipeline.Contexts;
    using Transports;
    using Queuing;

    /// <summary>
    /// Not for public consumption. May change in minor version releases.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class DispatchMessageToTransportBehavior : IBehavior<SendPhysicalMessageContext>
    {
        ISendMessages messageSender;
        IPublishMessages messagePublisher;
        IDeferMessages messageDeferral;

        public DispatchMessageToTransportBehavior(IDeferMessages messageDeferral, IPublishMessages messagePublisher, ISendMessages messageSender)
        {
            this.messageDeferral = messageDeferral;
            this.messagePublisher = messagePublisher;
            this.messageSender = messageSender;
        }

        public void Invoke(SendPhysicalMessageContext context, Action next)
        {
            var sendOptions = context.SendOptions;

            var logicalMessage = context.LogicalMessages.FirstOrDefault();
            var messageDescription = "ControlMessage";

            if (logicalMessage != null)
            {
                messageDescription = logicalMessage.MessageType.FullName;
            }

            context.MessageToSend.Headers[Headers.TimeSent] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);

            try
            {
                if (sendOptions.Intent == MessageIntentEnum.Publish)
                {
                    if (messagePublisher == null)
                    {
                        throw new InvalidOperationException("No message publisher has been registered. If you're using a transport without native support for pub/sub please enable the message driven publishing feature by calling: Feature.Enable<MessageDrivenPublisher>() in your configuration");
                    }

                    var eventTypesToPublish = context.LogicalMessages.SelectMany(m => m.Metadata.MessageHierarchy)
                        .Distinct()
                        .ToList();

                    var subscribersFound = messagePublisher.Publish(context.MessageToSend, eventTypesToPublish);

                    context.Set("SubscribersFound", subscribersFound);
                }
                else
                {
                    var deliverAt = DateTime.UtcNow;

                    if (sendOptions.DelayDeliveryWith.HasValue)
                    {
                        deliverAt = deliverAt + sendOptions.DelayDeliveryWith.Value;
                    }

                    if (sendOptions.DeliverAt.HasValue)
                    {
                        deliverAt = sendOptions.DeliverAt.Value;
                    }

                    if (deliverAt > DateTime.UtcNow)
                    {
                        context.MessageToSend.Headers[Headers.IsDeferredMessage] = true.ToString();

                        SetDelayDeliveryWithHeader(context, sendOptions.DelayDeliveryWith);

                        messageDeferral.Defer(context.MessageToSend, deliverAt, sendOptions.Destination);
                    }
                    else
                    {
                        messageSender.Send(context.MessageToSend, sendOptions.Destination);    
                    }
                }
            }
            catch (QueueNotFoundException ex)
            {
                throw new ConfigurationErrorsException("The destination queue '" + sendOptions.Destination +
                                                       "' could not be found. You may have misconfigured the destination for this kind of message (" +
                                                       messageDescription +
                                                       ") in the MessageEndpointMappings of the UnicastBusConfig section in your configuration file. " +
                                                       "It may also be the case that the given queue just hasn't been created yet, or has been deleted."
                    , ex);
            }

            if (Log.IsDebugEnabled)
            {
                Log.Debug(string.Format("Sending message {0} with ID {1} to destination {2}.\n" +
                                        "ToString() of the message yields: {3}\n" +
                                        "Message headers:\n{4}",
                    messageDescription,
                    context.MessageToSend.Id,
                    sendOptions.Destination,
                    logicalMessage != null ? logicalMessage.ToString() : "",
                    string.Join(", ", context.MessageToSend.Headers.Select(h => h.Key + ":" + h.Value).ToArray())
                    ));

            }

            next();
        }

        [ObsoleteEx(RemoveInVersion = "5.0",Message ="V 5.0 will have a explicit IDeferMessages.Defer method for this")]
        static void SetDelayDeliveryWithHeader(SendPhysicalMessageContext context, TimeSpan? delay)
        {
            if (!delay.HasValue)
                return;

            context.MessageToSend.Headers["NServiceBus.Temporary.DelayDeliveryWith"] = delay.Value.ToString();
        }

        static ILog Log = LogManager.GetLogger(typeof(DispatchMessageToTransportBehavior));
    }
}