namespace NServiceBus.Unicast.Queuing.Msmq
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Messaging;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Xml.Serialization;
    using Transport;

    ///<summary>
    /// MSMQ-related utility functions
    ///</summary>
    public class MsmqUtilities
    {
        private static string accountToBeAssignedQueuePermissions;

        /// <summary>
        /// Sets the account to be assigned queue permissions.
        /// </summary>
        /// <param name="account">Account to be used.</param>
        public static void AccountToBeAssignedQueuePermissions(string account)
        {
            accountToBeAssignedQueuePermissions = account;
        }

        private static string getFullPath(string value)
        {
            return GetFullPath(Address.Parse(value));
        }

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
        /// Returns the full path without Format or direct os
        /// from a '@' separated path.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetFullPathWithoutPrefix(string value)
        {
            return getMachineNameFromLogicalName(value) + PRIVATE + getQueueNameFromLogicalName(value);
        }



        private static string getMachineNameFromLogicalName(string logicalName)
        {
            string[] arr = logicalName.Split('@');

            string machine = Environment.MachineName;

            if (arr.Length >= 2)
                if (arr[1] != "." && arr[1].ToLower() != "localhost")
                    machine = arr[1];

            return machine;
        }

        private static string getQueueNameFromLogicalName(string logicalName)
        {
            string[] arr = logicalName.Split('@');

            if (arr.Length >= 1)
                return arr[0];

            return null;
        }

        /// <summary>
        /// Checks whether or not a queue is local by its path.
        /// </summary>
        /// <param name="value">The path to the queue to check.</param>
        /// <returns>true if the queue is local, otherwise false.</returns>
        public static bool QueueIsLocal(string value)
        {
            var machineName = Environment.MachineName.ToLower();

            value = value.ToLower().Replace(PREFIX.ToLower(), "");
            var index = value.IndexOf('\\');

            var queueMachineName = value.Substring(0, index).ToLower();

            return (machineName == queueMachineName || queueMachineName == "localhost" || queueMachineName == ".");
        }

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
        /// Returns the number of messages in the queue.
        /// </summary>
        /// <returns></returns>
        public static int GetNumberOfPendingMessages(string queueName)
        {
            var q = new MessageQueue(getFullPath(queueName));
            
            var qMgmt = new MSMQ.MSMQManagementClass();

            object machine = Environment.MachineName;
            var missing = Type.Missing;
            object formatName = q.FormatName;

            qMgmt.Init(ref machine, ref missing, ref formatName);
            return qMgmt.MessageCount;
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
                         : m.CorrelationId),
                Recoverable = m.Recoverable,
                TimeToBeReceived = m.TimeToBeReceived,
                ReplyToAddress = GetIndependentAddressForQueue(m.ResponseQueue),
                MessageIntent = Enum.IsDefined(typeof(MessageIntentEnum), m.AppSpecific) ? (MessageIntentEnum)m.AppSpecific : MessageIntentEnum.Send
            };

            m.BodyStream.Position = 0;
            result.Body = new byte[m.BodyStream.Length];
            m.BodyStream.Read(result.Body, 0, result.Body.Length);

            result.Headers = new Dictionary<string, string>();
            if (m.Extension.Length > 0)
            {
                var stream = new MemoryStream(m.Extension);
                var o = headerSerializer.Deserialize(stream);

                foreach (var pair in o as List<Utils.HeaderInfo>)
                    if (pair.Key != null)
                        result.Headers.Add(pair.Key, pair.Value);
            }

            result.Id = result.GetOriginalId();
            if (result.Headers.ContainsKey("EnclosedMessageTypes")) // This is a V2.6 message
                ExtractMsmqMessageLabelInformationForBackwardCompatibility(m, result);
            result.IdForCorrelation = result.GetIdForCorrelation();

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

            if (msmqMsg.Label.Contains(TransportHeaderKeys.IdForCorrelation))
            {
                int idStartIndex = msmqMsg.Label.IndexOf(string.Format("<{0}>", TransportHeaderKeys.IdForCorrelation)) + TransportHeaderKeys.IdForCorrelation.Length + 2;
                int idCount = msmqMsg.Label.IndexOf(string.Format("</{0}>", TransportHeaderKeys.IdForCorrelation)) - idStartIndex;

                result.IdForCorrelation = msmqMsg.Label.Substring(idStartIndex, idCount);
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
                result.BodyStream = new MemoryStream(message.Body);

            if (message.CorrelationId != null)
                result.CorrelationId = message.CorrelationId;

            result.Recoverable = message.Recoverable;

            if (message.TimeToBeReceived < MessageQueue.InfiniteTimeout)
                result.TimeToBeReceived = message.TimeToBeReceived;

            if (message.Headers == null)
                message.Headers = new Dictionary<string, string>();

            if (!message.Headers.ContainsKey(TransportHeaderKeys.IdForCorrelation))
                message.Headers.Add(TransportHeaderKeys.IdForCorrelation, null);

            if (String.IsNullOrEmpty(message.Headers[TransportHeaderKeys.IdForCorrelation]))
                message.Headers[TransportHeaderKeys.IdForCorrelation] = message.IdForCorrelation;

            using (var stream = new MemoryStream())
            {
                headerSerializer.Serialize(stream, message.Headers.Select(pair => new Utils.HeaderInfo { Key = pair.Key, Value = pair.Value }).ToList());
                result.Extension = stream.GetBuffer();
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
                string.Format("<{0}>{2}</{0}><{1}>{3}</{1}>", TransportHeaderKeys.IdForCorrelation, Headers.WindowsIdentityName,
                    transportMessage.IdForCorrelation, windowsIdentityName);
        }

        private const string DIRECTPREFIX = "DIRECT=OS:";
        private static readonly string DIRECTPREFIX_TCP = "DIRECT=TCP:";
        private readonly static string PREFIX_TCP = "FormatName:" + DIRECTPREFIX_TCP;
        private static readonly string PREFIX = "FormatName:" + DIRECTPREFIX;
        private static readonly XmlSerializer headerSerializer = new XmlSerializer(typeof(List<Utils.HeaderInfo>));
        internal const string PRIVATE = "\\private$\\";
    }
}
