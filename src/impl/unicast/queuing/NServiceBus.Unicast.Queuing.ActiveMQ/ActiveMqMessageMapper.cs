namespace NServiceBus.Unicast.Queuing.ActiveMQ
{
    using System;
    using System.Collections.Generic;

    using Apache.NMS;
    using Apache.NMS.Util;

    public class ActiveMqMessageMapper : IActiveMqMessageMapper
    {
        private static string MessageIntentKey = "MessageIntent";

        public IMessage CreateJmsMessage(TransportMessage message, INetTxSession session)
        {
            if (message.Headers == null || !message.Headers.ContainsKey(Headers.EnclosedMessageTypes))
            {
                throw new ArgumentException("Messages must have the enclosed message type on the header.");
            }

            var jmsmessage = session.CreateBytesMessage(message.Body);

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
            jmsmessage.NMSReplyTo = SessionUtil.GetDestination(session, "queue://" + message.ReplyToAddress.Queue);
            jmsmessage.Properties[MessageIntentKey] = (int)message.MessageIntent;

            foreach (var header in message.Headers)
            {
                jmsmessage.Properties[header.Key] = header.Value;
            }

            return jmsmessage;
        }

        public TransportMessage CreateTransportMessage(IMessage message)
        {
            var transportMessage = new TransportMessage
                {
                    MessageIntent = (MessageIntentEnum)message.Properties[MessageIntentKey],
                    ReplyToAddress = new Address(message.NMSReplyTo.ToString(), string.Empty),
                    CorrelationId = message.NMSCorrelationID,
                    IdForCorrelation = message.NMSCorrelationID,
                    TimeToBeReceived = message.NMSTimeToLive,
                    Id = message.NMSMessageId,
                    Body = ((IBytesMessage)message).Content,
                    Headers = new Dictionary<string,string>()
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