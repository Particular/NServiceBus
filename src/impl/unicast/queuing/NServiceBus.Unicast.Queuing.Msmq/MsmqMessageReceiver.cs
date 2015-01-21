namespace NServiceBus.Unicast.Queuing.Msmq
{
    using System;
    using System.Diagnostics;
    using System.Messaging;
    using System.Security.Principal;
    using Common.Logging;
    using Transport;
    using Transport.Transactional.Config;
    using Utils;

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
                throw new InvalidOperationException(string.Format("Input queue [{0}] must be on the same machine as this process [{1}].",
                    address, Environment.MachineName.ToLower()));

            myQueue = new MessageQueue(MsmqUtilities.GetFullPath(address));
            errorQueue = new MessageQueue(MsmqUtilities.GetFullPath(ErrorQueue), false, true, QueueAccessMode.Send);

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
                using (var m = myQueue.Peek(TimeSpan.FromSeconds(secondsToWait)))
                {
                    return m != null;
                }
            }
            catch (MessageQueueException mqe)
            {
                if (mqe.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                    return false;
                string errorMessage;
                switch(mqe.MessageQueueErrorCode)
                {
                    case MessageQueueErrorCode.AccessDenied:
                        errorMessage = string.Format("Do not have permission to access queue [{0}]. Make sure that the current user [{1}] has permission to Send, Receive, and Peek  from this queue. Exception: [{2}]",
                            myQueue.FormatName, WindowsIdentity.GetCurrent() != null ? WindowsIdentity.GetCurrent().Name : "unknown user", mqe);
                        break;
                    
                    case MessageQueueErrorCode.QueueNotFound:
                        errorMessage = string.Format("Queue [{0}] was not found while peeking queue. Exception: [{1}]", myQueue.FormatName, mqe);
                        break;
                    
                    default:
                        errorMessage = string.Format("Error while while peeking queue: [{0}], exception: [{1}]", myQueue.FormatName, mqe);        
                        break;
                }
                Logger.Fatal(errorMessage);
                throw new InvalidOperationException(errorMessage, mqe);
            }
            catch (ObjectDisposedException objectDisposedException)
            {
                var errorMessage = string.Format("Queue has been disposed. Cannot continue operation. Please restart this process. Exception: {0}", objectDisposedException);
                Logger.Fatal(errorMessage);
                throw new InvalidOperationException(errorMessage, objectDisposedException);
            }
            catch (Exception e)
            {
                Logger.Fatal(e);
                throw;
            }
        }

        public TransportMessage Receive()
        {
            try
            {
                var transactionType = GetTransactionTypeForReceive();
                using (var m = myQueue.Receive(TimeSpan.FromSeconds(secondsToWait), transactionType))
                {
                    if (m == null)
                    {
                        return null;
                    }
                    try
                    {
                        return MsmqUtilities.Convert(m);
                    }
                    catch (Exception exception)
                    {
                        LogCorruptedMessage(m, exception);
                        errorQueue.Send(m, transactionType);
                        return null;
                    }
                }
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

        void LogCorruptedMessage(Message message, Exception ex)
        {
            var error = string.Format("Message '{0}' is corrupt and will be moved to '{1}'", message.Id, ErrorQueue.Queue);
            Logger.Error(error, ex);
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

            //in 4.0 this line would be Endpoint.DontUseDistributedTransactions
            if(Bootstrapper.SupressDTC)
                return MessageQueueTransactionType.Single;

            return MessageQueueTransactionType.Automatic;
        }

        /// <summary>
        /// Sets whether or not the transport should purge the input
        /// queue when it is started.
        /// </summary>
        public bool PurgeOnStartup { get; set; }

        public int SecondsToWaitForMessage
        {
            get { return secondsToWait;  }
            set { secondsToWait = value; }
        }

        /// <summary>
        /// The address of the configured error queue. 
        /// </summary>
        public Address ErrorQueue { get; set; }

        private int secondsToWait = 1;

        private MessageQueue myQueue;
        MessageQueue errorQueue;

        private bool useTransactions;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(MsmqMessageReceiver));
    }
}