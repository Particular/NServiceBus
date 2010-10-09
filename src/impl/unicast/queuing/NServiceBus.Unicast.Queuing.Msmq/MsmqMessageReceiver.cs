using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Messaging;
using System.Security.Principal;
using System.Threading;
using System.Xml.Serialization;
using NServiceBus.Unicast.Transport;
using NServiceBus.Utils;
using Common.Logging;

namespace NServiceBus.Unicast.Queuing.Msmq
{
    public class MsmqMessageReceiver : IReceiveMessages
    {
        public void Init(string queue)
        {
            if (string.IsNullOrEmpty(queue))
                throw new ArgumentException("Input queue must be specified");

            var machine = MsmqUtilities.GetMachineNameFromLogicalName(queue);

            if (machine.ToLower() != Environment.MachineName.ToLower())
                throw new InvalidOperationException("Input queue must be on the same machine as this process.");

            MsmqUtilities.CreateQueueIfNecessary(queue);

            myQueue = new MessageQueue(MsmqUtilities.GetFullPath(queue));

            bool transactional;
            try
            {
                transactional = myQueue.Transactional;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format("There is a problem with the input queue given: {0}. See the enclosed exception for details.", queue), ex);
            }

            if (!transactional)
                throw new ArgumentException("Queue must be transactional (" + queue + ").");

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
                    Logger.Fatal(string.Format("Do not have permission to access queue [{0}]. Make sure that the current user [{1}] has permission to Send, Receive, and Peek  from this queue. NServiceBus will now exit.", myQueue.QueueName, WindowsIdentity.GetCurrent() != null ? WindowsIdentity.GetCurrent().Name : "unknown user"));
                    Thread.Sleep(10000); //long enough for someone to notice
                    System.Diagnostics.Process.GetCurrentProcess().Kill();
                }

                throw;
            }
            catch(ObjectDisposedException)
            {
                Logger.Fatal("Queue has been disposed. Cannot continue operation. Please restart this process.");
                Thread.Sleep(10000); //long enough for someone to notice
                System.Diagnostics.Process.GetCurrentProcess().Kill();

                throw;
            }
        }

        public TransportMessage Receive(bool transactional)
        {
            try
            {
                var m = myQueue.Receive(TimeSpan.FromSeconds(secondsToWait), GetTransactionTypeForReceive(transactional));
                if (m == null)
                    return null;

                var result = new TransportMessage
                {
                    Id = m.Id,
                    CorrelationId =
                        (m.CorrelationId == "00000000-0000-0000-0000-000000000000\\0"
                             ? null
                             : m.CorrelationId),
                    Recoverable = m.Recoverable,
                    TimeToBeReceived = m.TimeToBeReceived,
                    TimeSent = m.SentTime,
                    ReturnAddress = MsmqUtilities.GetIndependentAddressForQueue(m.ResponseQueue),
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

                    foreach(var pair in o as List<HeaderInfo>)
                        if (pair.Key != null)
                            result.Headers.Add(pair.Key, pair.Value);
                }

                result.IdForCorrelation = GetIdForCorrelation(result.Headers);
                if (result.IdForCorrelation == null)
                    result.IdForCorrelation = result.Id;

                return result;
            }
            catch (MessageQueueException mqe)
            {
                if (mqe.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                    return null;

                if (mqe.MessageQueueErrorCode == MessageQueueErrorCode.AccessDenied)
                {
                    Logger.Fatal(string.Format("Do not have permission to access queue [{0}]. Make sure that the current user [{1}] has permission to Send, Receive, and Peek  from this queue. NServiceBus will now exit.", myQueue.QueueName, WindowsIdentity.GetCurrent() != null ? WindowsIdentity.GetCurrent().Name : "unknown user"));
                    Thread.Sleep(10000); //long enough for someone to notice
                    System.Diagnostics.Process.GetCurrentProcess().Kill();
                }

                throw;
            }
        }

        private static string GetIdForCorrelation(IDictionary<string, string> headers)
        {
            if (headers.ContainsKey(HeaderKeys.IDFORCORRELATION))
                return headers[HeaderKeys.IDFORCORRELATION];

            return null;
        }

        private static MessageQueueTransactionType GetTransactionTypeForReceive(bool transactional)
        {
            return transactional ? MessageQueueTransactionType.Automatic : MessageQueueTransactionType.None;
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

        private readonly XmlSerializer headerSerializer = new XmlSerializer(typeof(List<HeaderInfo>));

        private static readonly ILog Logger = LogManager.GetLogger(typeof(MsmqMessageReceiver));
    }
}