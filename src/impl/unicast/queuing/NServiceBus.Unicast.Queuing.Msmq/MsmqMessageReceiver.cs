using System;
using System.Diagnostics;
using System.Messaging;
using System.Security.Principal;
using NServiceBus.Unicast.Transport;
using NServiceBus.Utils;
using Common.Logging;

namespace NServiceBus.Unicast.Queuing.Msmq
{
    public class MsmqMessageReceiver : IReceiveMessages
    {
        public void Init(string address, bool transactional)
        {
            Init(Address.Parse(address), transactional);
        }

        public void Init(Address address, bool transactional)
        {
            useTransactions = transactional;

            if (address == null)
                throw new ArgumentException("Input queue must be specified");

            var machine = address.Machine;

            if (machine.ToLower() != Environment.MachineName.ToLower())
                throw new InvalidOperationException("Input queue must be on the same machine as this process.");

            myQueue = new MessageQueue(MsmqUtilities.GetFullPath(address));

            if (useTransactions && !QueueIsTransactional())
                throw new ArgumentException("Queue must be transactional (" + address + ").");

            var mpf = new MessagePropertyFilter();
            mpf.SetAll();

            myQueue.MessageReadPropertyFilter = mpf;

            if (PurgeOnStartup)
                myQueue.Purge();
        }

      
        [DebuggerNonUserCode]
        public bool HasMessage()
        {
            try
            {
                var m = myQueue.Peek(TimeSpan.FromSeconds(secondsToWait));
                return m != null;
            }
            catch (MessageQueueException mqe)
            {
                if (mqe.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                    return false;

                if (mqe.MessageQueueErrorCode == MessageQueueErrorCode.AccessDenied)
                {
                    Logger.Fatal(string.Format("Do not have permission to access queue [{0}]. Make sure that the current user [{1}] has permission to Send, Receive, and Peek  from this queue.", myQueue.QueueName, WindowsIdentity.GetCurrent() != null ? WindowsIdentity.GetCurrent().Name : "unknown user"));
                    Configure.Instance.OnCriticalError(mqe);
                }

                throw;
            }
            catch (ObjectDisposedException objectDisposedException)
            {
                Logger.Fatal("Queue has been disposed. Cannot continue operation. Please restart this process.");
                Configure.Instance.OnCriticalError(objectDisposedException);
                throw;
            }
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
                    Logger.Fatal(string.Format("Do not have permission to access queue [{0}]. Make sure that the current user [{1}] has permission to Send, Receive, and Peek  from this queue.", myQueue.QueueName, WindowsIdentity.GetCurrent() != null ? WindowsIdentity.GetCurrent().Name : "unknown user"));
                    Configure.Instance.OnCriticalError(mqe);
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
                throw new InvalidOperationException(string.Format("There is a problem with the input queue given: {0}. See the enclosed exception for details.", myQueue.QueueName), ex);
            }
        }
        
        private MessageQueueTransactionType GetTransactionTypeForReceive()
        {
            return useTransactions ? MessageQueueTransactionType.Automatic : MessageQueueTransactionType.None;
        }


        /// <summary>
        /// Sets whether or not the transport should purge the input
        /// queue when it is started.
        /// </summary>
        public bool PurgeOnStartup { get; set; }


        private int secondsToWait = 1;
        public int SecondsToWaitForMessage
        {
            get { return secondsToWait;  }
            set { secondsToWait = value; }
        }

        private MessageQueue myQueue;

        private bool useTransactions;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(MsmqMessageReceiver));
    }
}