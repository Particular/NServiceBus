using System;
using System.Messaging;
using System.Security.Principal;
using NServiceBus.Logging;

namespace NServiceBus.Unicast.Queuing.Msmq
{
    using Config;

    public class MsmqMessageReceiver : IReceiveMessages
    {
        public void Init(Address address, bool transactional)
        {
            useTransactions = transactional;

            if (address == null)
                throw new ArgumentException("Input queue must be specified");

            var machine = address.Machine;

            if (machine.ToLower() != Environment.MachineName.ToLower())
                throw new InvalidOperationException(string.Format("Input queue [{0}] must be on the same machine as this process [{1}].",
                    address, Environment.MachineName.ToLower()));

            myQueue = new MessageQueue(MsmqUtilities.GetFullPath(address));

            if (useTransactions && !QueueIsTransactional())
                throw new ArgumentException("Queue must be transactional (" + address + ").");

            var mpf = new MessagePropertyFilter();
            mpf.SetAll();

            myQueue.MessageReadPropertyFilter = mpf;

            if (PurgeOnStartup)
                myQueue.Purge();
        }


        bool IReceiveMessages.HasMessage()
        {
            return true;
        }

        public TransportMessage Receive()
        {
            try
            {
                var m = myQueue.Receive(TimeSpan.FromSeconds(secondsToWait), GetTransactionTypeForReceive());
                if (m == null)
                    return null;

                return MsmqUtilities.Convert(m);
            }
            catch (MessageQueueException mqe)
            {
                if (mqe.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                    return null;

                if (mqe.MessageQueueErrorCode == MessageQueueErrorCode.AccessDenied)
                {
                    string errorException = string.Format("Do not have permission to access queue [{0}]. Make sure that the current user [{1}] has permission to Send, Receive, and Peek  from this queue.", myQueue.QueueName, WindowsIdentity.GetCurrent() != null ? WindowsIdentity.GetCurrent().Name : "unknown user");
                    Logger.Fatal(errorException);
                    throw new InvalidOperationException(errorException, mqe);
                }

                throw;
            }
        }

        bool QueueIsTransactional()
        {
            try
            {
                return myQueue.Transactional;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format("There is a problem with the input queue: {0}. See the enclosed exception for details.", myQueue.Path), ex);
            }
        }
        
        private MessageQueueTransactionType GetTransactionTypeForReceive()
        {
            if(!useTransactions)
                return MessageQueueTransactionType.None;

            if(Endpoint.DontUseDistributedTransactions)
                return MessageQueueTransactionType.Single;

            return MessageQueueTransactionType.Automatic;
        }


        /// <summary>
        /// Sets whether or not the transport should purge the input
        /// queue when it is started.
        /// </summary>
        public bool PurgeOnStartup { get; set; }


        int secondsToWait = 1;
        public int SecondsToWaitForMessage
        {
            get { return secondsToWait;  }
            set { secondsToWait = value; }
        }

        MessageQueue myQueue;

        bool useTransactions;

        static readonly ILog Logger = LogManager.GetLogger(typeof(MsmqMessageReceiver));
    }
}