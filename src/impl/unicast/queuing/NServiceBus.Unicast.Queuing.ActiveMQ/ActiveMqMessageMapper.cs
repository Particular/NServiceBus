namespace NServiceBus.Unicast.Queuing.ActiveMQ
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using Apache.NMS;

    public class ActiveMqMessageMapper : IActiveMqMessageMapper
    {
        internal const string MessageIntentKey = "MessageIntent";

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
            jmsmessage.NMSType = message.Headers[Headers.EnclosedMessageTypes];
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
            var messageIntentEnum = (MessageIntentEnum)message.Properties[MessageIntentKey];
            var replyToAddress = new Address(message.NMSReplyTo.ToString(), string.Empty);
            byte[] body = Encoding.UTF8.GetBytes(((ITextMessage)message).Text);

            var transportMessage = new TransportMessage
                {
                    MessageIntent = messageIntentEnum,
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

            return transportMessage;
        }
    }
}