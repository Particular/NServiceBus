namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Messaging;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Xml;
    using System.Xml.Serialization;

    ///<summary>
    /// MSMQ-related utility functions
    ///</summary>
    public class MsmqUtilities
    {
        /// <summary>
        /// Turns a '@' separated value into a full path.
        /// Format is 'queue@machine', or 'queue@ipaddress'
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetFullPath(Address value)
        {
            IPAddress ipAddress;
            if (IPAddress.TryParse(value.Machine, out ipAddress))
                return PREFIX_TCP + MsmqQueueCreator.GetFullPathWithoutPrefix(value);

            return PREFIX + MsmqQueueCreator.GetFullPathWithoutPrefix(value);
        }

        /// <summary>
        /// Gets the name of the return address from the provided value.
        /// If the target includes a machine name, uses the local machine name in the returned value
        /// otherwise uses the local IP address in the returned value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static string GetReturnAddress(string value, string target)
        {
            return GetReturnAddress(Address.Parse(value), Address.Parse(target));
        }

        /// <summary>
        /// Gets the name of the return address from the provided value.
        /// If the target includes a machine name, uses the local machine name in the returned value
        /// otherwise uses the local IP address in the returned value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static string GetReturnAddress(Address value, Address target)
        {
            var machine = target.Machine;

            IPAddress targetIpAddress;

            //see if the target is an IP address, if so, get our own local ip address
            if (IPAddress.TryParse(machine, out targetIpAddress))
            {
                if (string.IsNullOrEmpty(localIp))
                    localIp = LocalIpAddress(targetIpAddress);

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
                    .SelectMany(ni=>ni.GetIPProperties().UnicastAddresses).ToList();

            var firstWithMatchingFamily =
                availableAddresses.FirstOrDefault(a => a.Address.AddressFamily == targetIpAddress.AddressFamily);

            if (firstWithMatchingFamily != null)
                return firstWithMatchingFamily.Address.ToString();

            var fallbackToDifferentFamily = availableAddresses.FirstOrDefault();

            if (fallbackToDifferentFamily != null)
                return fallbackToDifferentFamily.Address.ToString();

            return "127.0.0.1";
        }

        static string localIp;

        /// <summary>
        /// Gets an independent address for the queue in the form:
        /// queue@machine.
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public static Address GetIndependentAddressForQueue(MessageQueue q)
        {
            if (q == null)
                return null;

            string[] arr = q.FormatName.Split('\\');
            string queueName = arr[arr.Length - 1];

            int directPrefixIndex = arr[0].IndexOf(DIRECTPREFIX);
            if (directPrefixIndex >= 0)
                return new Address(queueName, arr[0].Substring(directPrefixIndex + DIRECTPREFIX.Length));

            int tcpPrefixIndex = arr[0].IndexOf(DIRECTPREFIX_TCP);
            if (tcpPrefixIndex >= 0)
                return new Address(queueName, arr[0].Substring(tcpPrefixIndex + DIRECTPREFIX_TCP.Length));

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
        /// Converts an MSMQ message to a TransportMessage.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static TransportMessage Convert(Message m)
        {
            var result = new TransportMessage
            {
                Id = m.Id,
                CorrelationId =
                    (m.CorrelationId == "00000000-0000-0000-0000-000000000000\\0"
                         ? null
                         : m.CorrelationId.Replace("\\0","")), //msmq required the id's to be in the {guid}\{incrementing number} format so we need to fake a \0 at the end that the sender added to make it compatible
                Recoverable = m.Recoverable,
                TimeToBeReceived = m.TimeToBeReceived,
                ReplyToAddress = GetIndependentAddressForQueue(m.ResponseQueue)
            };

            if (Enum.IsDefined(typeof (MessageIntentEnum), m.AppSpecific))
                result.MessageIntent = (MessageIntentEnum) m.AppSpecific;

            m.BodyStream.Position = 0;
            result.Body = new byte[m.BodyStream.Length];
            m.BodyStream.Read(result.Body, 0, result.Body.Length);

            if (m.Extension.Length > 0)
            {
                object o;
                using (var stream = new MemoryStream(m.Extension))
                using (var reader = XmlReader.Create(stream, new XmlReaderSettings {CheckCharacters = false}))
                {
                    o = headerSerializer.Deserialize(reader);
                }

                foreach (var pair in o as List<HeaderInfo>)
                {
                    if (pair.Key != null)
                    {
                        result.Headers[pair.Key] = pair.Value;
                    }
                }
            }

            if (result.Headers.ContainsKey("EnclosedMessageTypes")) // This is a V2.6 message
            {
                ExtractMsmqMessageLabelInformationForBackwardCompatibility(m, result);
            }
       
            return result;
        }

        /// <summary>
        /// For backward compatibility, extract the V2.6 MSMQ label content (IdForCorrelation and WindowsIdentityName) 
        /// into the V3.X transport message.
        /// </summary>
        /// <param name="msmqMsg">Received MSMQ message</param>
        /// <param name="result">Transport message to be filled from MSMQ message label</param>
        private static void ExtractMsmqMessageLabelInformationForBackwardCompatibility(Message msmqMsg, TransportMessage result)
        {
            if (string.IsNullOrWhiteSpace(msmqMsg.Label))
                return;

            if (msmqMsg.Label.Contains("CorrId"))
            {
                int idStartIndex = msmqMsg.Label.IndexOf(string.Format("<{0}>", "CorrId")) + "CorrId".Length + 2;
                int idCount = msmqMsg.Label.IndexOf(string.Format("</{0}>", "CorrId")) - idStartIndex;

                result.Headers["CorrId"] = msmqMsg.Label.Substring(idStartIndex, idCount);
            }

            if (msmqMsg.Label.Contains(Headers.WindowsIdentityName) && !result.Headers.ContainsKey(Headers.WindowsIdentityName))
            {
                int winStartIndex = msmqMsg.Label.IndexOf(string.Format("<{0}>", Headers.WindowsIdentityName)) + Headers.WindowsIdentityName.Length + 2;
                int winCount = msmqMsg.Label.IndexOf(string.Format("</{0}>", Headers.WindowsIdentityName)) - winStartIndex;

                result.Headers.Add(Headers.WindowsIdentityName, msmqMsg.Label.Substring(winStartIndex, winCount));
            }
        }

        /// <summary>
        /// Converts a TransportMessage to an Msmq message.
        /// Doesn't set the ResponseQueue of the result.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Message Convert(TransportMessage message)
        {
            var result = new Message();

            if (message.Body != null)
            {
                result.BodyStream = new MemoryStream(message.Body);
            }

            result.CorrelationId = message.CorrelationId + "\\0";//msmq required the id's to be in the {guid}\{incrementing number} format so we need to fake a \0 at the end to make it compatible

            result.Recoverable = message.Recoverable;

            if (message.TimeToBeReceived < MessageQueue.InfiniteTimeout)
            {
                result.TimeToBeReceived = message.TimeToBeReceived;
            }

            using (var stream = new MemoryStream())
            {
                headerSerializer.Serialize(stream, message.Headers.Select(pair => new HeaderInfo { Key = pair.Key, Value = pair.Value }).ToList());
                result.Extension = stream.ToArray();
            }

            result.AppSpecific = (int)message.MessageIntent;

            FillLabelForBackwardsCompatabilityWhileSending(message, result);

            return result;
        }
        /// <summary>
        /// Fill MSMQ message's label to be compatible with NServiceBus V2.6
        /// </summary>
        /// <param name="transportMessage"></param>
        /// <param name="msmqMessage"></param>
        static void FillLabelForBackwardsCompatabilityWhileSending(TransportMessage transportMessage, Message msmqMessage)
        {
            string windowsIdentityName =
                (transportMessage.Headers.ContainsKey(Headers.WindowsIdentityName) && (!string.IsNullOrWhiteSpace(transportMessage.Headers[Headers.WindowsIdentityName])))
                ? transportMessage.Headers[Headers.WindowsIdentityName] : string.Empty;

            msmqMessage.Label =
                string.Format("<{0}>{2}</{0}><{1}>{3}</{1}>", "CorrId", Headers.WindowsIdentityName,
                    transportMessage.Id, windowsIdentityName);
        }

        private const string DIRECTPREFIX = "DIRECT=OS:";
        private static readonly string DIRECTPREFIX_TCP = "DIRECT=TCP:";
        private readonly static string PREFIX_TCP = "FormatName:" + DIRECTPREFIX_TCP;
        private static readonly string PREFIX = "FormatName:" + DIRECTPREFIX;
        private static readonly XmlSerializer headerSerializer = new XmlSerializer(typeof(List<HeaderInfo>));
        internal const string PRIVATE = "\\private$\\";
    }
}
