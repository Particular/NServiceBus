namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Extensibility;
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

        public OutgoingMessage GetOutgoingMessage(Context context)
        {
            var state = context.Extensions.GetOrCreate<State>();

            state.Headers[Headers.MessageIntent] = context.Intent.ToString();

            return new OutgoingMessage(state.MessageId, state.Headers, context.Body);
        }

        public void InvokeNative(Context context)
        {
            var message = GetOutgoingMessage(context);

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

        public class State
        {
            public State()
            {
                Headers = new Dictionary<string, string>();
                MessageId = CombGuid.Generate().ToString();
            }
            public Dictionary<string, string> Headers { get; private set; }
            public string MessageId { get; set; }
        }
    }

    /// <summary>
    /// Extensions to the outgoing pipeline
    /// </summary>
    public static class DispatchContextExtensions
    {
        /// <summary>
        /// Allows headers to be set
        /// </summary>
        /// <param name="context">Context to extend</param>
        /// <param name="key">The header key</param>
        /// <param name="value">The header value</param>
        public static void SetHeader(this OutgoingContext context, string key, string value)
        {
            context.Extensions.GetOrCreate<DispatchMessageToTransportBehavior.State>()
                .Headers[key] = value;
        }

        /// <summary>
        /// Allows headers to be set
        /// </summary>
        /// <param name="context">Context to extend</param>
        /// <param name="key">The header key</param>
        /// <param name="value">The header value</param>
        public static void SetHeader(this ExtendableOptions context, string key, string value)
        {
            context.Extensions.GetOrCreate<DispatchMessageToTransportBehavior.State>()
                .Headers[key] = value;
        }

        /// <summary>
        /// Allows headers to be set
        /// </summary>
        /// <param name="context">Context to extend</param>
        /// <param name="key">The header key</param>
        /// <param name="value">The header value</param>
        public static void SetHeader(this PhysicalOutgoingContextStageBehavior.Context context, string key, string value)
        {
            context.Extensions.GetOrCreate<DispatchMessageToTransportBehavior.State>()
                .Headers[key] = value;
        }

        /// <summary>
        /// Returns the id for this message
        /// </summary>
        /// <param name="context">Context beeing extended</param>
        /// <returns>The message id</returns>
        public static string GetMessageId(this PhysicalOutgoingContextStageBehavior.Context context)
        {
            return context.Extensions.GetOrCreate<DispatchMessageToTransportBehavior.State>().MessageId;
        }
        /// <summary>
        /// Returns the id for this message
        /// </summary>
        /// <param name="context">Context beeing extended</param>
        /// <returns>The message id</returns>
        public static string GetMessageId(this OutgoingContext context)
        {
            return context.Extensions.GetOrCreate<DispatchMessageToTransportBehavior.State>().MessageId;
        }

        /// <summary>
        /// Allows the user to set the message id
        /// </summary>
        /// <param name="context">Context to extend</param>
        /// <param name="messageId">The message id to use</param>
        public static void SetMessageId(this ExtendableOptions context, string messageId)
        {
            Guard.AgainstNullAndEmpty(messageId,messageId);

            context.Extensions.GetOrCreate<DispatchMessageToTransportBehavior.State>()
                .MessageId = messageId;
        }

    }
}