namespace NServiceBus.Unicast.Queuing.ActiveMQ
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Apache.NMS;

    public class ActiveMqMessageMapper : IActiveMqMessageMapper
    {
        public const string MessageIntentKey = "MessageIntent";
        private readonly IMessageTypeInterpreter messageTypeInterpreter;

        public ActiveMqMessageMapper(IMessageTypeInterpreter messageTypeInterpreter)
        {
            this.messageTypeInterpreter = messageTypeInterpreter;
        }

        public IMessage CreateJmsMessage(TransportMessage message, INetTxSession session)
        {
            if (message.Headers == null || !message.Headers.ContainsKey(Headers.EnclosedMessageTypes))
            {
                throw new ArgumentException("Messages must have the enclosed message type on the header.");
            }

            string messageBody = Encoding.UTF8.GetString(message.Body);
            IMessage jmsmessage = session.CreateTextMessage(messageBody);

            if (message.IdForCorrelation != null)
            {
                jmsmessage.NMSCorrelationID = message.IdForCorrelation;
            }

            if (message.TimeToBeReceived < TimeSpan.FromMilliseconds(uint.MaxValue))
            {
                jmsmessage.NMSTimeToLive = message.TimeToBeReceived;
            }

            jmsmessage.NMSDeliveryMode = message.Recoverable ? MsgDeliveryMode.Persistent : MsgDeliveryMode.NonPersistent;
            jmsmessage.NMSType = message.Headers[Headers.EnclosedMessageTypes].Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            jmsmessage.NMSReplyTo = session.GetQueue(message.ReplyToAddress.Queue);
            jmsmessage.Properties[MessageIntentKey] = (int)message.MessageIntent;

            foreach (var header in message.Headers)
            {
                jmsmessage.Properties[header.Key] = header.Value;
            }

            return jmsmessage;
        }

        public TransportMessage CreateTransportMessage(IMessage message)
        {
            var replyToAddress = message.NMSReplyTo == null ? null : new Address(message.NMSReplyTo.ToString(), string.Empty);
            byte[] body = Encoding.UTF8.GetBytes(((ITextMessage)message).Text);

            var transportMessage = new TransportMessage
                {
                    MessageIntent = this.GetIntent(message),
                    ReplyToAddress = replyToAddress,
                    CorrelationId = message.NMSCorrelationID,
                    IdForCorrelation = message.NMSCorrelationID,
                    TimeToBeReceived = message.NMSTimeToLive,
                    Recoverable = message.NMSDeliveryMode == MsgDeliveryMode.Persistent,
                    Id = message.NMSMessageId,
                    Body = body,
                    Headers = new Dictionary<string, string>()
                };

            foreach (var key in message.Properties.Keys)
            {
                var keyString = (string)key;
                if (keyString == MessageIntentKey)
                {
                    continue;
                }

                transportMessage.Headers[keyString] = message.Properties[keyString].ToString();
            }

            if (!transportMessage.Headers.ContainsKey(Headers.EnclosedMessageTypes))
            {
                transportMessage.Headers[Headers.EnclosedMessageTypes] = 
                    this.messageTypeInterpreter.GetAssemblyQualifiedName(message.NMSType);
            }

            if (!transportMessage.Headers.ContainsKey(Headers.NServiceBusVersion))
            {
                transportMessage.Headers[Headers.NServiceBusVersion] = "3.0.0.0";
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