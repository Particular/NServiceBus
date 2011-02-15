using System;
using System.Messaging;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Principal;
using Common.Logging;

namespace NServiceBus.Utils
{
    ///<summary>
    /// MSMQ-related utility functions
    ///</summary>
    public class MsmqUtilities
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MsmqUtilities));
        private static readonly string LocalAdministratorsGroupName = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null).Translate(typeof(NTAccount)).ToString();
        private static readonly string LocalEveryoneGroupName = new SecurityIdentifier(WellKnownSidType.WorldSid, null).Translate(typeof(NTAccount)).ToString();
        private static readonly string LocalAnonymousLogonName = new SecurityIdentifier(WellKnownSidType.AnonymousSid, null).Translate(typeof(NTAccount)).ToString();

        ///<summary>
        /// Utility method for creating a queue if it does not exist.
        ///</summary>
        ///<param name="queueName"></param>
        ///<param name="account">The account to be given permissions to the queue</param>
        public static void CreateQueueIfNecessary(string queueName, string account)
        {
            if (string.IsNullOrEmpty(queueName))
                return;

            var q = GetFullPathWithoutPrefix(queueName);
            var machine = GetMachineNameFromLogicalName(queueName);

            if (machine != Environment.MachineName)
            {
                Logger.Debug("Queue is on remote machine.");
                Logger.Debug("If this does not succeed (like if the remote machine is disconnected), processing will continue.");
            }

            Logger.Debug(string.Format("Checking if queue exists: {0}.", queueName));

            try
            {
                if (MessageQueue.Exists(q))
                {
                    Logger.Debug("Queue exists, going to set permissions.");
                    SetPermissionsForQueue(q, account);
                    return;
                }

                Logger.Warn("Queue " + q + " does not exist.");
                Logger.Debug("Going to create queue: " + q);

                CreateQueue(q, account);
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("Could not create queue {0} or check its existence. Processing will still continue.", queueName), ex);
            }
        }
        
        ///<summary>
        /// Create named message queue
        ///</summary>
        ///<param name="queueName"></param>
        ///<param name="account">The account to be given permissions to the queue</param>
        public static void CreateQueue(string queueName, string account)
        {
            var createdQueue = MessageQueue.Create(queueName, true);

            SetPermissionsForQueue(queueName, account);
            
            Logger.Debug("Queue created: " + queueName);
        }

        /// <summary>
        /// Sets default permissions for queue.
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="account"></param>
        public static void SetPermissionsForQueue(string queue, string account)
        {
            var q = new MessageQueue(queue);

            q.SetPermissions(LocalAdministratorsGroupName, MessageQueueAccessRights.FullControl, AccessControlEntryType.Allow);
            q.SetPermissions(LocalEveryoneGroupName, MessageQueueAccessRights.WriteMessage, AccessControlEntryType.Allow);
            q.SetPermissions(LocalAnonymousLogonName, MessageQueueAccessRights.WriteMessage, AccessControlEntryType.Allow);

            q.SetPermissions(account, MessageQueueAccessRights.WriteMessage, AccessControlEntryType.Allow);
            q.SetPermissions(account, MessageQueueAccessRights.ReceiveMessage, AccessControlEntryType.Allow);
            q.SetPermissions(account, MessageQueueAccessRights.PeekMessage, AccessControlEntryType.Allow);            
        }

        /// <summary>
        /// Turns a '@' separated value into a full path.
        /// Format is 'queue@machine', or 'queue@ipaddress'
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetFullPath(string value)
        {
            var machine = GetMachineNameFromLogicalName(value);

            IPAddress ipAddress;
            if (IPAddress.TryParse(machine, out ipAddress))
				return PREFIX_TCP + GetFullPathWithoutPrefix(value);

            return PREFIX + GetFullPathWithoutPrefix(value);
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
            var machine = GetMachineNameFromLogicalName(target);

            IPAddress ipAddress;

            //see if the target is an IP address, if so, get our own local ip address
            if (IPAddress.TryParse(machine, out ipAddress))
            {
                string myIp = null;

                var networkInterfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
                foreach(var ni in networkInterfaces)
                    if (ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        var ipProps = ni.GetIPProperties();
                        if (ipProps.UnicastAddresses.Count > 0)
                        {
                            myIp = ipProps.UnicastAddresses[1].Address.ToString();
                            break;
                        }
                    }

                if (myIp == null)
                    myIp = "127.0.0.1";

                return PREFIX_TCP + myIp + PRIVATE + GetQueueNameFromLogicalName(value);
            }

            return PREFIX + GetFullPathWithoutPrefix(value);
        }

        /// <summary>
        /// Returns the full path without Format or direct os
        /// from a '@' separated path.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetFullPathWithoutPrefix(string value)
        {
            return GetMachineNameFromLogicalName(value) + PRIVATE + GetQueueNameFromLogicalName(value);
        }

        /// <summary>
        /// Returns the machine name from a '@' separated full logical name,
        /// or the Environment.MachineName otherwise.
        /// </summary>
        /// <param name="logicalName"></param>
        /// <returns></returns>
        public static string GetMachineNameFromLogicalName(string logicalName)
        {
            string[] arr = logicalName.Split('@');

            string machine = Environment.MachineName;

            if (arr.Length >= 2)
                if (arr[1] != "." && arr[1].ToLower() != "localhost")
                    machine = arr[1];

            return machine;
        }

        /// <summary>
        /// Returns the queue name from a '@' separated full logical name.
        /// </summary>
        /// <param name="logicalName"></param>
        /// <returns></returns>
        public static string GetQueueNameFromLogicalName(string logicalName)
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
        public static string GetIndependentAddressForQueue(MessageQueue q)
        {
            if (q == null)
                return null;

            string[] arr = q.FormatName.Split('\\');
            string queueName = arr[arr.Length - 1];

            int directPrefixIndex = arr[0].IndexOf(DIRECTPREFIX);
            if (directPrefixIndex >= 0)
                return queueName + '@' + arr[0].Substring(directPrefixIndex + DIRECTPREFIX.Length);

            int tcpPrefixIndex = arr[0].IndexOf(DIRECTPREFIX_TCP);
            if (tcpPrefixIndex >= 0)
                return queueName + '@' + arr[0].Substring(tcpPrefixIndex + DIRECTPREFIX_TCP.Length);

            try
            {
                // the pessimistic approach failed, try the optimistic approach
                arr = q.QueueName.Split('\\');
                queueName = arr[arr.Length - 1];
                return queueName + '@' + q.MachineName;
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
            var q = new MessageQueue(GetFullPath(queueName));

            var qMgmt = new MSMQ.MSMQManagementClass();
            object machine = Environment.MachineName;
            var missing = Type.Missing;
            object formatName = q.FormatName;

            qMgmt.Init(ref machine, ref missing, ref formatName);
            return qMgmt.MessageCount;
        }

        private const string DIRECTPREFIX = "DIRECT=OS:";
        private static readonly string DIRECTPREFIX_TCP = "DIRECT=TCP:";
        private readonly static string PREFIX_TCP = "FormatName:" + DIRECTPREFIX_TCP;
        private static readonly string PREFIX = "FormatName:" + DIRECTPREFIX;
        private const string PRIVATE = "\\private$\\";
    }
}