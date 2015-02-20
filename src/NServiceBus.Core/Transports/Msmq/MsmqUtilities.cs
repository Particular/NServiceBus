namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Messaging;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Text;
    using System.Xml;
    using Logging;
    using NServiceBus.Support;
    using Transports.Msmq;

    /// <summary>
    ///     MSMQ-related utility functions
    /// </summary>
    class MsmqUtilities
    {
        /// <summary>
        ///     Turns a '@' separated value into a full path.
        ///     Format is 'queue@machine', or 'queue@ipaddress'
        /// </summary>
        public static string GetFullPath(Address value)
        {
            IPAddress ipAddress;
            if (IPAddress.TryParse(value.Machine, out ipAddress))
            {
                return PREFIX_TCP + MsmqQueueCreator.GetFullPathWithoutPrefix(value);
            }

            return PREFIX + MsmqQueueCreator.GetFullPathWithoutPrefix(value);
        }

        public static string GetFullPath(string queue)
        {
            return PREFIX + MsmqQueueCreator.GetFullPathWithoutPrefix(queue,RuntimeEnvironment.MachineName);
        }

        /// <summary>
        ///     Gets the name of the return address from the provided value.
        ///     If the target includes a machine name, uses the local machine name in the returned value
        ///     otherwise uses the local IP address in the returned value.
        /// </summary>
        public static string GetReturnAddress(string value, string target)
        {
            return GetReturnAddress(Address.Parse(value), Address.Parse(target));
        }

        /// <summary>
        ///     Gets the name of the return address from the provided value.
        ///     If the target includes a machine name, uses the local machine name in the returned value
        ///     otherwise uses the local IP address in the returned value.
        /// </summary>
        public static string GetReturnAddress(Address value, Address target)
        {
            var machine = target.Machine;

            IPAddress targetIpAddress;

            //see if the target is an IP address, if so, get our own local ip address
            if (IPAddress.TryParse(machine, out targetIpAddress))
            {
                if (string.IsNullOrEmpty(localIp))
                {
                    localIp = LocalIpAddress(targetIpAddress);
                }

                return PREFIX_TCP + localIp + PRIVATE + value.Queue;
            }

            return PREFIX + MsmqQueueCreator.GetFullPathWithoutPrefix(value);
        }

        static string LocalIpAddress(IPAddress targetIpAddress)
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            var availableAddresses =
                networkInterfaces.Where(
                    ni =>
                        ni.OperationalStatus == OperationalStatus.Up &&
                        ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .SelectMany(ni => ni.GetIPProperties().UnicastAddresses).ToList();

            var firstWithMatchingFamily =
                availableAddresses.FirstOrDefault(a => a.Address.AddressFamily == targetIpAddress.AddressFamily);

            if (firstWithMatchingFamily != null)
            {
                return firstWithMatchingFamily.Address.ToString();
            }

            var fallbackToDifferentFamily = availableAddresses.FirstOrDefault();

            if (fallbackToDifferentFamily != null)
            {
                return fallbackToDifferentFamily.Address.ToString();
            }

            return "127.0.0.1";
        }

       
        static Address GetIndependentAddressForQueue(MessageQueue q)
        {
            if (q == null)
            {
                return null;
            }

            var arr = q.FormatName.Split('\\');
            var queueName = arr[arr.Length - 1];

            var directPrefixIndex = arr[0].IndexOf(DIRECTPREFIX);
            if (directPrefixIndex >= 0)
            {
                return new Address(queueName, arr[0].Substring(directPrefixIndex + DIRECTPREFIX.Length));
            }

            var tcpPrefixIndex = arr[0].IndexOf(DIRECTPREFIX_TCP);
            if (tcpPrefixIndex >= 0)
            {
                return new Address(queueName, arr[0].Substring(tcpPrefixIndex + DIRECTPREFIX_TCP.Length));
            }

            try
            {
                // the pessimistic approach failed, try the optimistic approach
                arr = q.QueueName.Split('\\');
                queueName = arr[arr.Length - 1];
                return new Address(queueName, q.MachineName);
            }
            catch
            {
                throw new Exception("Could not translate format name to independent name: " + q.FormatName);
            }
        }

        /// <summary>
        ///     Converts an MSMQ message to a TransportMessage.
        /// </summary>
        public static TransportMessage Convert(Message m)
        {
            var headers = DeserializeMessageHeaders(m);

            
            var result = new TransportMessage(m.Id, headers)
            {
                Recoverable = m.Recoverable,
                TimeToBeReceived = m.TimeToBeReceived,
                CorrelationId = GetCorrelationId(m, headers)
            };

            //note: we can drop this line when we no longer support interop btw v3 + v4
            if (m.ResponseQueue != null)
            {
                result.Headers[Headers.ReplyToAddress] = GetIndependentAddressForQueue(m.ResponseQueue).ToString();    
            }
            

            if (Enum.IsDefined(typeof(MessageIntentEnum), m.AppSpecific))
            {
                result.MessageIntent = (MessageIntentEnum) m.AppSpecific;
            }

            m.BodyStream.Position = 0;
            result.Body = new byte[m.BodyStream.Length];
            m.BodyStream.Read(result.Body, 0, result.Body.Length);

            return result;
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
            return message.CorrelationId.Replace("\\0", "");
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

        /// <summary>
        ///     Converts a TransportMessage to an Msmq message.
        ///     Doesn't set the ResponseQueue of the result.
        /// </summary>
        public static Message Convert(TransportMessage message)
        {
            var result = new Message();

            if (message.Body != null)
            {
                result.BodyStream = new MemoryStream(message.Body);
            }


            AssignMsmqNativeCorrelationId(message, result);

            result.Recoverable = message.Recoverable;

            if (message.TimeToBeReceived < MessageQueue.InfiniteTimeout)
            {
                result.TimeToBeReceived = message.TimeToBeReceived;
            }

            using (var stream = new MemoryStream())
            {
                headerSerializer.Serialize(stream, message.Headers.Select(pair => new HeaderInfo
                {
                    Key = pair.Key,
                    Value = pair.Value
                }).ToList());
                result.Extension = stream.ToArray();
            }

            result.AppSpecific = (int) message.MessageIntent;

            return result;
        }

        static void AssignMsmqNativeCorrelationId(TransportMessage message, Message result)
        {
            if (string.IsNullOrEmpty(message.CorrelationId))
            {
                return;
            }

            Guid correlationId;

            if (Guid.TryParse(message.CorrelationId, out correlationId))
            {
                //msmq required the id's to be in the {guid}\{incrementing number} format so we need to fake a \0 at the end to make it compatible                
                result.CorrelationId = message.CorrelationId + "\\0";
                return;
            }

            try
            {
                if (message.CorrelationId.Contains("\\"))
                {
                    var parts = message.CorrelationId.Split('\\');

                    int number;

                    if (parts.Count() == 2 && Guid.TryParse(parts.First(), out correlationId) &&
                        int.TryParse(parts[1], out number))
                    {
                        result.CorrelationId = message.CorrelationId;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to assign a native correlation id for message: " + message.Id, ex);
            }
        }

        const string DIRECTPREFIX = "DIRECT=OS:";
        const string DIRECTPREFIX_TCP = "DIRECT=TCP:";
        const string PREFIX_TCP = "FormatName:" + DIRECTPREFIX_TCP;
        const string PREFIX = "FormatName:" + DIRECTPREFIX;
        internal const string PRIVATE = "\\private$\\";
        static string localIp;
        static System.Xml.Serialization.XmlSerializer headerSerializer = new System.Xml.Serialization.XmlSerializer(typeof(List<HeaderInfo>));
        static ILog Logger = LogManager.GetLogger<MsmqUtilities>();
    }
}
