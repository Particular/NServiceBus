using System.Messaging;
using System.Security.Principal;
using Common.Logging;

namespace NServiceBus.Utils
{
    ///<summary>
    /// MSMQ-related utility functions
    ///</summary>
    public class MsmqUtilities
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(MsmqUtilities));
        private static readonly string localAdministratorsGroupName;

        static MsmqUtilities()
        {
            localAdministratorsGroupName = new SecurityIdentifier("S-1-5-32-544").Translate(typeof(NTAccount)).ToString();
        }

        ///<summary>
        /// Utility method for creating a queue if it does not exist.
        ///</summary>
        ///<param name="queueName"></param>
        public static void CreateQueueIfNecessary(string queueName)
        {
            logger.Debug("Checking if queue exists.");
            
            if (!MessageQueue.Exists(queueName))
            {
                logger.Warn("Queue " + queueName + " does not exist.");
                logger.Debug("Going to create queue: " + queueName);

                CreateQueue(queueName);
            }
        }
        
        ///<summary>
        /// Create named message queue
        ///</summary>
        ///<param name="queueName"></param>
        public static void CreateQueue(string queueName)
        {
            var createdQueue = MessageQueue.Create(queueName, true);

            createdQueue.SetPermissions(localAdministratorsGroupName, MessageQueueAccessRights.FullControl, AccessControlEntryType.Allow);

            logger.Debug("Queue created: " + queueName);
        }
    }
}