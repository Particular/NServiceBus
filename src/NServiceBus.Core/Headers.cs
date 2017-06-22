namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    /// <summary>
    /// Static class containing headers used by NServiceBus.
    /// </summary>
    public static class Headers
    {
        /// <summary>
        /// Header for retrieving from which Http endpoint the message arrived.
        /// </summary>
        [HeaderId(1)] public const string HttpFrom = "NServiceBus.From";

        /// <summary>
        /// Header for specifying to which Http endpoint the message should be delivered.
        /// </summary>
        [HeaderId(2)] public const string HttpTo = "NServiceBus.To";

        /// <summary>
        /// Header for specifying to which queue behind the http gateway should the message be delivered.
        /// This header is considered an applicative header.
        /// </summary>
        [HeaderId(3)] public const string RouteTo = "NServiceBus.Header.RouteTo";

        /// <summary>
        /// Header for specifying to which sites the gateway should send the message. For multiple
        /// sites a comma separated list can be used
        /// This header is considered an applicative header.
        /// </summary>
        [HeaderId(4)] public const string DestinationSites = "NServiceBus.DestinationSites";

        /// <summary>
        /// Header for specifying the key for the site where this message originated.
        /// This header is considered an applicative header.
        /// </summary>
        [HeaderId(5)] public const string OriginatingSite = "NServiceBus.OriginatingSite";

        /// <summary>
        /// Header containing the id of the saga instance the sent the message
        /// This header is considered an applicative header.
        /// </summary>
        [HeaderId(6)] public const string SagaId = "NServiceBus.SagaId";

        /// <summary>
        /// Header containing a stable message id for a message.
        /// </summary>
        [HeaderId(7)] public const string MessageId = "NServiceBus.MessageId";

        /// <summary>
        /// Header containing a correlation id for a message.
        /// </summary>
        [HeaderId(8)] public const string CorrelationId = "NServiceBus.CorrelationId";

        /// <summary>
        /// Header containing the ReplyToAddress for a message.
        /// </summary>
        [HeaderId(9)] public const string ReplyToAddress = "NServiceBus.ReplyToAddress";

        /// <summary>
        /// Prefix included on the wire when sending applicative headers.
        /// </summary>
        [HeaderId(10)] public const string HeaderName = "Header";

        /// <summary>
        /// Header telling the NServiceBus Version (beginning NServiceBus V3.0.1).
        /// </summary>
        [HeaderId(11)] public const string NServiceBusVersion = "NServiceBus.Version";

        /// <summary>
        /// Used in a header when doing a callback (session.return).
        /// </summary>
        [HeaderId(12)] public const string ReturnMessageErrorCodeHeader = "NServiceBus.ReturnMessage.ErrorCode";

        /// <summary>
        /// Header that tells if this transport message is a control message.
        /// </summary>
        [HeaderId(13)] public const string ControlMessageHeader = "NServiceBus.ControlMessage";

        /// <summary>
        /// Type of the saga that this message is targeted for.
        /// </summary>
        [HeaderId(14)] public const string SagaType = "NServiceBus.SagaType";

        /// <summary>
        /// Id of the saga that sent this message.
        /// </summary>
        [HeaderId(15)] public const string OriginatingSagaId = "NServiceBus.OriginatingSagaId";

        /// <summary>
        /// Type of the saga that sent this message.
        /// </summary>
        [HeaderId(16)] public const string OriginatingSagaType = "NServiceBus.OriginatingSagaType";

        /// <summary>
        /// The number of Delayed Retries that have been performed for this message.
        /// </summary>
        [HeaderId(17)] public const string DelayedRetries = "NServiceBus.Retries";

        /// <summary>
        /// The time the last Delayed Retry has been performed for this message.
        /// </summary>
        [HeaderId(18)] public const string DelayedRetriesTimestamp = "NServiceBus.Retries.Timestamp";

        /// <summary>
        /// The number of Immediate Retries that have been performed for this message.
        /// </summary>
        [HeaderId(19)] public const string ImmediateRetries = "NServiceBus.FLRetries";

        /// <summary>
        /// The time processing of this message started.
        /// </summary>
        [HeaderId(20)] public const string ProcessingStarted = "NServiceBus.ProcessingStarted";

        /// <summary>
        /// The time processing of this message ended.
        /// </summary>
        [HeaderId(21)] public const string ProcessingEnded = "NServiceBus.ProcessingEnded";

        /// <summary>
        /// The time this message was sent from the client.
        /// </summary>
        [HeaderId(22)] public const string TimeSent = "NServiceBus.TimeSent";

        /// <summary>
        /// Id of the message that caused this message to be sent.
        /// </summary>
        [HeaderId(23)] public const string RelatedTo = "NServiceBus.RelatedTo";

        /// <summary>
        /// Header entry key indicating the types of messages contained.
        /// </summary>
        [HeaderId(24)] public const string EnclosedMessageTypes = "NServiceBus.EnclosedMessageTypes";

        /// <summary>
        /// Header entry key indicating format of the payload.
        /// </summary>
        [HeaderId(25)] public const string ContentType = "NServiceBus.ContentType";

        /// <summary>
        /// Header entry key for the given message type that is being subscribed to, when message intent is subscribe or
        /// unsubscribe.
        /// </summary>
        [HeaderId(26)] public const string SubscriptionMessageType = "SubscriptionMessageType";

        /// <summary>
        /// Header entry key for the transport address of the subscribing endpoint.
        /// </summary>
        [HeaderId(27)] public const string SubscriberTransportAddress = "NServiceBus.SubscriberAddress";

        /// <summary>
        /// Header entry key for the logical name of the subscribing endpoint.
        /// </summary>
        [HeaderId(28)] public const string SubscriberEndpoint = "NServiceBus.SubscriberEndpoint";

        /// <summary>
        /// True if this message is a saga timeout.
        /// </summary>
        [HeaderId(29)] public const string IsSagaTimeoutMessage = "NServiceBus.IsSagaTimeoutMessage";

        /// <summary>
        /// True if this is a deferred message.
        /// </summary>
        [HeaderId(30)] public const string IsDeferredMessage = "NServiceBus.IsDeferredMessage";

        /// <summary>
        /// Name of the endpoint where the given message originated.
        /// </summary>
        [HeaderId(31)] public const string OriginatingEndpoint = "NServiceBus.OriginatingEndpoint";

        /// <summary>
        /// Machine name of the endpoint where the given message originated.
        /// </summary>
        [HeaderId(32)] public const string OriginatingMachine = "NServiceBus.OriginatingMachine";

        /// <summary>
        /// HostId of the endpoint where the given message originated.
        /// </summary>
        [HeaderId(33)] public const string OriginatingHostId = "$.diagnostics.originating.hostid";

        /// <summary>
        /// Name of the endpoint where the given message was processed (success or failure).
        /// </summary>
        [HeaderId(34)] public const string ProcessingEndpoint = "NServiceBus.ProcessingEndpoint";

        /// <summary>
        /// Machine name of the endpoint where the given message was processed (success or failure).
        /// </summary>
        [HeaderId(35)] public const string ProcessingMachine = "NServiceBus.ProcessingMachine";

        /// <summary>
        /// The display name of the host where the given message was processed (success or failure), eg the MachineName.
        /// </summary>
        [HeaderId(36)] public const string HostDisplayName = "$.diagnostics.hostdisplayname";

        /// <summary>
        /// HostId of the endpoint where the given message was processed (success or failure).
        /// </summary>
        [HeaderId(37)] public const string HostId = "$.diagnostics.hostid";

        /// <summary>
        /// HostId of the endpoint where the given message was processed (success or failure).
        /// </summary>
        [HeaderId(38)] public const string HasLicenseExpired = "$.diagnostics.license.expired";

        /// <summary>
        /// The original reply to address for successfully processed messages.
        /// </summary>
        [HeaderId(39)] public const string OriginatingAddress = "NServiceBus.OriginatingAddress";

        /// <summary>
        /// The id of the message conversation that this message is part of.
        /// </summary>
        [HeaderId(40)] public const string ConversationId = "NServiceBus.ConversationId";

        /// <summary>
        /// The intent of the current message.
        /// </summary>
        [HeaderId(41)] public const string MessageIntent = "NServiceBus.MessageIntent";

        /// <summary>
        /// The identifier to lookup the key to decrypt the encrypted data.
        /// </summary>
        [HeaderId(42)] public const string RijndaelKeyIdentifier = "NServiceBus.RijndaelKeyIdentifier";

        /// <summary>
        /// The time to be received for this message when it was sent the first time.
        /// When moved to error and audit this header will be preserved to the original TTBR
        /// of the message can be known.
        /// </summary>
        [HeaderId(43)] public const string TimeToBeReceived = "NServiceBus.TimeToBeReceived";

        /// <summary>
        /// Indicates that the message was sent as a non-durable message.
        /// </summary>
        [HeaderId(44)] public const string NonDurableMessage = "NServiceBus.NonDurableMessage";
    }

    // Marks a known core header with an assigned byte identifier.
    [AttributeUsage(AttributeTargets.Field)]
    class HeaderIdAttribute : Attribute
    {
        static HeaderIdAttribute()
        {
            idToString = typeof(Headers).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic)
                .Where(fi => fi.IsLiteral && fi.IsInitOnly)
                .ToDictionary(fi => fi.GetCustomAttribute<HeaderIdAttribute>().Id, fi => (string)fi.GetValue(null));

            stringToId = idToString.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
        }

        public HeaderIdAttribute(byte id)
        {
            Id = id;
        }

        public static bool TryGetId(string headerName, out byte id) => stringToId.TryGetValue(headerName, out id);
        public static string GetName(byte id) => idToString[id];

        readonly byte Id;
        static Dictionary<byte, string> idToString;
        static Dictionary<string, byte> stringToId;
        public const byte NonStandard = byte.MaxValue;
    }

    /// <summary>
    /// A fast and efficient serializer of headers, providing this capability for transports that do not support headers natively 
    /// or want to minimize the cost (storage/time) of serializing them.
    /// </summary>
    public class HeadersFastEncoder
    {
        const byte Version = 1;
        const int LengthLength = 2;
        const int ChunkLength = 1024;

        static readonly UTF8Encoding Encoding = new UTF8Encoding(false);

        /// <summary>
        /// Reads the headers written previously by this serializer.
        /// </summary>
        public static Dictionary<string, string> Read(ArraySegment<byte> bytes)
        {
            var array = bytes.Array;
            var offset = bytes.Offset;
            var to = offset + bytes.Count;

            if (array[offset++] != Version)
            {
                throw new ArgumentException("Invalid version");
            }

            var result = new Dictionary<string, string>();
            while (offset < to)
            {
                var headerId = array[offset++];
                if (headerId != HeaderIdAttribute.NonStandard)
                {
                    var headerName = HeaderIdAttribute.GetName(headerId);
                    offset = ReadString(array, offset, out string headerValue);

                    result.Add(headerName, headerValue);
                }
                else
                {
                    offset = ReadString(array, offset, out string headerName);
                    offset = ReadString(array, offset, out string headerValue);

                    result.Add(headerName, headerValue);
                }
            }
            return result;
        }

        [ThreadStatic]
        static byte[] Chunk;

        /// <summary>
        /// Writes headers to a specified memory stream.
        /// </summary>
        public static void Write(Dictionary<string, string> values, MemoryStream destination)
        {
            var chunk = Chunk ?? new byte[ChunkLength];
            var offset = 0;

            chunk[offset++] = Version;
            try
            {
                foreach (var kvp in values)
                {
                    var headerName = kvp.Key;
                    byte id;
                    if (HeaderIdAttribute.TryGetId(headerName, out id))
                    {
                        var value = kvp.Value;
                        var predicted = 1; // 1 byte to write header id
                        predicted += LengthLength; // plus 2 bytes for length
                        var length = Encoding.GetByteCount(value);
                        predicted += length; // plus payload itself

                        Ensure(chunk, destination, predicted, ref offset);

                        chunk[offset++] = id;
                        offset = WriteString(chunk, (short)length, offset, value);
                    }
                    else
                    {
                        // write key
                        {
                            var key = kvp.Key;
                            var predicted = 1; // 1 byte to write header id
                            predicted += LengthLength; // plus 2 bytes for length
                            var length = Encoding.GetByteCount(key);
                            predicted += length; // plus payload itself

                            Ensure(chunk, destination, predicted, ref offset);

                            chunk[offset++] = HeaderIdAttribute.NonStandard;
                            offset = WriteString(chunk, (short)length, offset, key);
                        }

                        // write value
                        {
                            var value = kvp.Value;
                            var predicted = LengthLength; // 2 bytes for length
                            var length = Encoding.GetByteCount(value);
                            predicted += length; // plus payload itself

                            Ensure(chunk, destination, predicted, ref offset);

                            offset = WriteString(chunk, (short)length, offset, value);
                        }
                    }
                }
                
                // flush leftovers
                if (offset > 0)
                {
                    destination.Write(chunk, 0, offset);
                }
            }
            finally
            {
                Chunk = chunk;
            }
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        static void Ensure(byte[] chunk, MemoryStream destination, int predicted, ref int offset)
        {
            var left = ChunkLength - offset;
            if (left >= predicted)
            {
                return;
            }

            destination.Write(chunk, 0, offset);
            offset = 0;
        }


        /// <summary>
        /// Writes string with length prefix.
        /// </summary>
        /// <returns>New offset.</returns>
        static int WriteString(byte[] chunk, short length, int offset, string value)
        {
            var o = offset;
            chunk[o++] = (byte)(length & byte.MaxValue);
            chunk[o++] = (byte)((length>>8) & byte.MaxValue);

            Encoding.GetBytes(value, 0, value.Length, chunk, o);

            o += length;
            return o;
        }

        static int ReadString(byte[] array, int offset, out string value)
        {
            var o = offset;

            var valueLength = array[o++] + (array[o++] >> 8);
            value = Encoding.GetString(array, o, valueLength);
            o += valueLength;

            return o;
        }
    }
}