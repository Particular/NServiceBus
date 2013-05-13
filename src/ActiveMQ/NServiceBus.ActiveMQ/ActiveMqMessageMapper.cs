namespace NServiceBus.Transports.ActiveMQ
{
    using System;
    using System.Linq;
    using Apache.NMS;
    using Apache.NMS.Util;

    using NServiceBus.Serialization;

    public class ActiveMqMessageMapper : IActiveMqMessageMapper
    {
        public const string ErrorCodeKey = "ErrorCode";

        readonly IMessageSerializer serializer;

        private readonly IMessageTypeInterpreter messageTypeInterpreter;

        private readonly IActiveMqMessageEncoderPipeline encoderPipeline;

        private readonly IActiveMqMessageDecoderPipeline decoderPipeline;

        public ActiveMqMessageMapper(IMessageSerializer serializer, IMessageTypeInterpreter messageTypeInterpreter, IActiveMqMessageEncoderPipeline encoderPipeline, IActiveMqMessageDecoderPipeline decoderPipeline)
        {
            this.serializer = serializer;
            this.messageTypeInterpreter = messageTypeInterpreter;
            this.encoderPipeline = encoderPipeline;
            this.decoderPipeline = decoderPipeline;
        }

        public IMessage CreateJmsMessage(TransportMessage message, ISession session)
        {
            IMessage jmsmessage = this.encoderPipeline.Encode(message, session);

            jmsmessage.NMSMessageId = message.Id;

            jmsmessage.NMSCorrelationID = message.CorrelationId;

            if (message.TimeToBeReceived < TimeSpan.FromMilliseconds(uint.MaxValue))
            {
                jmsmessage.NMSTimeToLive = message.TimeToBeReceived;
            }

            if (message.Headers.ContainsKey(Headers.EnclosedMessageTypes))
            {
                jmsmessage.NMSType = message.Headers[Headers.EnclosedMessageTypes].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            }

            jmsmessage.NMSDeliveryMode = message.Recoverable ? MsgDeliveryMode.Persistent : MsgDeliveryMode.NonPersistent;

            if (message.ReplyToAddress != null && message.ReplyToAddress != Address.Undefined)
            {
                jmsmessage.NMSReplyTo = SessionUtil.GetQueue(session, message.ReplyToAddress.Queue);
            }

            foreach (var header in message.Headers)
            {
                jmsmessage.Properties[ConvertMessageHeaderKeyToActiveMQ(header.Key)] = header.Value;
            }

            return jmsmessage;
        }

        public TransportMessage CreateTransportMessage(IMessage message)
        {
            var transportMessage = new TransportMessage();

            this.decoderPipeline.Decode(transportMessage, message);

            var replyToAddress = message.NMSReplyTo == null
                                     ? null
                                     : new Address(message.NMSReplyTo.ToString(), string.Empty);

            transportMessage.ReplyToAddress = replyToAddress;
            transportMessage.CorrelationId = message.NMSCorrelationID;
            transportMessage.TimeToBeReceived = message.NMSTimeToLive;
            transportMessage.Recoverable = message.NMSDeliveryMode == MsgDeliveryMode.Persistent;
            transportMessage.Id = message.NMSMessageId;

            foreach (var key in message.Properties.Keys)
            {
                var keyString = (string)key;
                if (keyString == ErrorCodeKey)
                {
                    continue;
                }
                
                transportMessage.Headers[ConvertMessageHeaderKeyFromActiveMQ(keyString)] = message.Properties[keyString] != null 
                                                                                               ? message.Properties[keyString].ToString() 
                                                                                               : null;
            }

            if (!transportMessage.Headers.ContainsKey(Headers.EnclosedMessageTypes))
            {
                var type = this.messageTypeInterpreter.GetAssemblyQualifiedName(message.NMSType);
                if (!string.IsNullOrEmpty(type))
                {
                    transportMessage.Headers[Headers.EnclosedMessageTypes] = type;
                }
            }

            if (!transportMessage.Headers.ContainsKey(Headers.ControlMessageHeader)
                && message.Properties.Contains(ErrorCodeKey))
            {
                transportMessage.Headers[Headers.ControlMessageHeader] = "true";
                transportMessage.Headers[Headers.ReturnMessageErrorCodeHeader] =
                    message.Properties[ErrorCodeKey].ToString();
            }

            if (!transportMessage.Headers.ContainsKey(Headers.NServiceBusVersion))
            {
                transportMessage.Headers[Headers.NServiceBusVersion] = "4.0.0.0";
            }

            if (!transportMessage.Headers.ContainsKey(Headers.ContentType))
            {
                transportMessage.Headers[Headers.ContentType] = this.serializer.ContentType;
            }

            return transportMessage;
        }

        public static string ConvertMessageHeaderKeyToActiveMQ(string headerKey)
        {
            return headerKey.Replace(".", "_DOT_").Replace("-", "_HYPHEN_");
        }

        public static string ConvertMessageHeaderKeyFromActiveMQ(string headerKey)
        {
            return headerKey.Replace("_DOT_", ".").Replace("_HYPHEN_", "-");
        }

    }
}