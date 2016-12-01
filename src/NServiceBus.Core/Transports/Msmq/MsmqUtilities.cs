namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Messaging;
    using System.Text;
    using System.Xml;
    using DeliveryConstraints;
    using Logging;
    using Performance.TimeToBeReceived;
    using Transport;
    using Transport.Msmq;

    class MsmqUtilities
    {
        static MsmqAddress GetIndependentAddressForQueue(MessageQueue q)
        {
            var arr = q.FormatName.Split('\\');
            var queueName = arr[arr.Length - 1];

            var directPrefixIndex = arr[0].IndexOf(DIRECTPREFIX);
            if (directPrefixIndex >= 0)
            {
                return new MsmqAddress(queueName, arr[0].Substring(directPrefixIndex + DIRECTPREFIX.Length));
            }

            var tcpPrefixIndex = arr[0].IndexOf(DIRECTPREFIX_TCP);
            if (tcpPrefixIndex >= 0)
            {
                return new MsmqAddress(queueName, arr[0].Substring(tcpPrefixIndex + DIRECTPREFIX_TCP.Length));
            }

            try
            {
                // the pessimistic approach failed, try the optimistic approach
                arr = q.QueueName.Split('\\');
                queueName = arr[arr.Length - 1];
                return new MsmqAddress(queueName, q.MachineName);
            }
            catch
            {
                throw new Exception($"Could not translate format name to independent name: {q.FormatName}");
            }
        }

        public static Dictionary<string, string> ExtractHeaders(Message msmqMessage)
        {
            var headers = DeserializeMessageHeaders(msmqMessage);

            //note: we can drop this line when we no longer support interop btw v3 + v4
            if (msmqMessage.ResponseQueue != null)
            {
                headers[Headers.ReplyToAddress] = GetIndependentAddressForQueue(msmqMessage.ResponseQueue).ToString();
            }

            if (Enum.IsDefined(typeof(MessageIntentEnum), msmqMessage.AppSpecific))
            {
                headers[Headers.MessageIntent] = ((MessageIntentEnum)msmqMessage.AppSpecific).ToString();
            }

            headers[Headers.CorrelationId] = GetCorrelationId(msmqMessage, headers);

            return headers;
        }

        static string GetCorrelationId(Message message, Dictionary<string, string> headers)
        {
            string correlationId;

            if (headers.TryGetValue(Headers.CorrelationId, out correlationId))
            {
                return correlationId;
            }

            if (message.CorrelationId == "00000000-0000-0000-0000-000000000000\\0")
            {
                return null;
            }

            //msmq required the id's to be in the {guid}\{incrementing number} format so we need to fake a \0 at the end that the sender added to make it compatible
            //The replace can be removed in v5 since only v3 messages will need this
            return message.CorrelationId.Replace("\\0", string.Empty);
        }

        static Dictionary<string, string> DeserializeMessageHeaders(Message m)
        {
            var result = new Dictionary<string, string>();

            if (m.Extension.Length == 0)
            {
                return result;
            }

            //This is to make us compatible with v3 messages that are affected by this bug:
            //http://stackoverflow.com/questions/3779690/xml-serialization-appending-the-0-backslash-0-or-null-character
            var extension = Encoding.UTF8.GetString(m.Extension).TrimEnd('\0');
            object o;
            using (var stream = new StringReader(extension))
            {
                using (var reader = XmlReader.Create(stream, new XmlReaderSettings
                {
                    CheckCharacters = false
                }))
                {
                    o = headerSerializer.Deserialize(reader);
                }
            }

            foreach (var pair in (List<HeaderInfo>)o)
            {
                if (pair.Key != null)
                {
                    result.Add(pair.Key, pair.Value);
                }
            }

            return result;
        }

        public static Message Convert(OutgoingMessage message, List<DeliveryConstraint> deliveryConstraints)
        {
            var result = new Message();

            if (message.Body != null)
            {
                result.BodyStream = new MemoryStream(message.Body);
            }


            AssignMsmqNativeCorrelationId(message, result);
            result.Recoverable = !deliveryConstraints.Any(c => c is NonDurableDelivery);

            DiscardIfNotReceivedBefore timeToBeReceived;

            if (deliveryConstraints.TryGet(out timeToBeReceived) && timeToBeReceived.MaxTime < MessageQueue.InfiniteTimeout)
            {
                result.TimeToBeReceived = timeToBeReceived.MaxTime;
            }

            var addCorrIdHeader = !message.Headers.ContainsKey("CorrId");

            using (var stream = new MemoryStream())
            {
                var headers = message.Headers.Select(pair => new HeaderInfo
                {
                    Key = pair.Key,
                    Value = pair.Value
                }).ToList();

                if (addCorrIdHeader)
                {
                    headers.Add(new HeaderInfo
                    {
                        Key = "CorrId",
                        Value = result.CorrelationId
                    });
                }

                headerSerializer.Serialize(stream, headers);
                result.Extension = stream.ToArray();
            }

            var messageIntent = default(MessageIntentEnum);

            string messageIntentString;

            if (message.Headers.TryGetValue(Headers.MessageIntent, out messageIntentString))
            {
                Enum.TryParse(messageIntentString, true, out messageIntent);
            }

            result.AppSpecific = (int)messageIntent;


            return result;
        }

        static void AssignMsmqNativeCorrelationId(OutgoingMessage message, Message result)
        {
            string correlationIdHeader;

            if (!message.Headers.TryGetValue(Headers.CorrelationId, out correlationIdHeader))
            {
                return;
            }

            if (string.IsNullOrEmpty(correlationIdHeader))
            {
                return;
            }

            Guid correlationId;

            if (Guid.TryParse(correlationIdHeader, out correlationId))
            {
                //msmq required the id's to be in the {guid}\{incrementing number} format so we need to fake a \0 at the end to make it compatible
                result.CorrelationId = $"{correlationIdHeader}\\0";
                return;
            }

            try
            {
                if (correlationIdHeader.Contains("\\"))
                {
                    var parts = correlationIdHeader.Split('\\');

                    int number;

                    if (parts.Length == 2 && Guid.TryParse(parts.First(), out correlationId) &&
                        int.TryParse(parts[1], out number))
                    {
                        result.CorrelationId = correlationIdHeader;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to assign a native correlation id for message: {message.MessageId}", ex);
            }
        }

        const string DIRECTPREFIX = "DIRECT=OS:";
        const string DIRECTPREFIX_TCP = "DIRECT=TCP:";
        internal const string PRIVATE = "\\private$\\";

        static System.Xml.Serialization.XmlSerializer headerSerializer = new System.Xml.Serialization.XmlSerializer(typeof(List<HeaderInfo>));
        static ILog Logger = LogManager.GetLogger<MsmqUtilities>();
    }
}