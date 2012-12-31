namespace NServiceBus.ActiveMQ
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Apache.NMS;
    using Apache.NMS.Util;

    using NServiceBus.Unicast.Transport;

    public class ActiveMqMessageMapper : IActiveMqMessageMapper
    {
        public const string MessageIntentKey = "MessageIntent";
        public const string ErrorCodeKey = "ErrorCode";
        
        private readonly IMessageTypeInterpreter messageTypeInterpreter;

        public ActiveMqMessageMapper(IMessageTypeInterpreter messageTypeInterpreter)
        {
            this.messageTypeInterpreter = messageTypeInterpreter;
        }

        public IMessage CreateJmsMessage(TransportMessage message, INetTxSession session)
        {
            IMessage jmsmessage = session.CreateTextMessage();

            if (!message.IsControlMessage())
            {
                string messageBody = Encoding.UTF8.GetString(message.Body);
                jmsmessage = session.CreateTextMessage(messageBody);
            }

            jmsmessage.NMSMessageId = message.Id;

            if (message.CorrelationId != null)
            {
                jmsmessage.NMSCorrelationID = message.CorrelationId;
            }

            if (message.TimeToBeReceived < TimeSpan.FromMilliseconds(uint.MaxValue))
            {
                jmsmessage.NMSTimeToLive = message.TimeToBeReceived;
            }

            if (message.Headers != null && message.Headers.ContainsKey(Headers.EnclosedMessageTypes))
            {
                jmsmessage.NMSType = message.Headers[Headers.EnclosedMessageTypes].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            }

            jmsmessage.NMSDeliveryMode = message.Recoverable ? MsgDeliveryMode.Persistent : MsgDeliveryMode.NonPersistent;

            if (message.ReplyToAddress != null && message.ReplyToAddress != Address.Undefined)
            {
                jmsmessage.NMSReplyTo = SessionUtil.GetQueue(session, message.ReplyToAddress.Queue);
            }

            jmsmessage.Properties[MessageIntentKey] = (int)message.MessageIntent;

            foreach (var header in message.Headers)
            {
                jmsmessage.Properties[header.Key] = header.Value;
            }

            return jmsmessage;
        }

        public TransportMessage CreateTransportMessage(IMessage message)
        {
            var replyToAddress = message.NMSReplyTo == null ? null : new Address(message.NMSReplyTo.ToString(), string.Empty, true);

            byte[] body = null;
            if (!message.IsControlMessage())
            {
                body = Encoding.UTF8.GetBytes(((ITextMessage)message).Text);
            }

            var transportMessage = new TransportMessage
                {
                    MessageIntent = this.GetIntent(message),
                    ReplyToAddress = replyToAddress,
                    CorrelationId = message.NMSCorrelationID,
                    TimeToBeReceived = message.NMSTimeToLive,
                    Recoverable = message.NMSDeliveryMode == MsgDeliveryMode.Persistent,
                    Id = message.NMSMessageId,
                    Body = body
                };

            foreach (var key in message.Properties.Keys)
            {
                var keyString = (string)key;
                if (keyString == MessageIntentKey || keyString == ErrorCodeKey)
                {
                    continue;
                }
                
                transportMessage.Headers[keyString] = message.Properties[keyString] != null ? message.Properties[keyString].ToString() : null;
            }

            if (!transportMessage.Headers.ContainsKey(Headers.EnclosedMessageTypes))
            {
                var type = this.messageTypeInterpreter.GetAssemblyQualifiedName(message.NMSType);
                if (!string.IsNullOrEmpty(type))
                {
                    transportMessage.Headers[Headers.EnclosedMessageTypes] = type;
                }
            }

            if (!transportMessage.Headers.ContainsKey(Headers.ControlMessageHeader) &&
                message.Properties.Contains(ErrorCodeKey))
            {
                transportMessage.Headers[Headers.ControlMessageHeader] = "true";
                transportMessage.Headers[Headers.ReturnMessageErrorCodeHeader] = message.Properties[ErrorCodeKey].ToString();
            }

            if (!transportMessage.Headers.ContainsKey(Headers.NServiceBusVersion))
            {
                transportMessage.Headers[Headers.NServiceBusVersion] = "4.0.0.0";
            }

            return transportMessage;
        }

        private MessageIntentEnum GetIntent(IMessage message)
        {
            var messageIntentProperty = message.Properties[MessageIntentKey];
            if (messageIntentProperty != null)
            {
                return (MessageIntentEnum)messageIntentProperty;
            }

            if (message.NMSDestination != null && message.NMSDestination.IsTopic)
            {
                return MessageIntentEnum.Publish;
            }

            return MessageIntentEnum.Send;
        }
    }
}