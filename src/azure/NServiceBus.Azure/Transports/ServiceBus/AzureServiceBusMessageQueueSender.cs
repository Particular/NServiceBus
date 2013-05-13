using System;
using System.Collections.Generic;
using System.Threading;
using System.Transactions;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    using Transports;

    /// <summary>
    /// 
    /// </summary>
    public class AzureServiceBusMessageQueueSender : ISendMessages
    {
        public const int DefaultBackoffTimeInSeconds = 10;

        private readonly Dictionary<string, QueueClient> senders = new Dictionary<string, QueueClient>();
        private static readonly object SenderLock = new Object();

        public TimeSpan LockDuration { get; set; }
        public long MaxSizeInMegabytes { get; set; }
        public bool RequiresDuplicateDetection { get; set; }
        public bool RequiresSession { get; set; }
        public TimeSpan DefaultMessageTimeToLive { get; set; }
        public bool EnableDeadLetteringOnMessageExpiration { get; set; }
        public TimeSpan DuplicateDetectionHistoryTimeWindow { get; set; }
        public int MaxDeliveryCount { get; set; }
        public bool EnableBatchedOperations { get; set; }

        public MessagingFactory Factory { get; set; }
        public NamespaceManager NamespaceClient { get; set; }

        public void Init(string address, bool transactional)
        {
            Init(Address.Parse(address), transactional);
        }

        public void Init(Address address, bool transactional)
        {

        }

        public void Send(TransportMessage message, string destination)
        {
            Send(message, Address.Parse(destination));
        }

        public void Send(TransportMessage message, Address address)
        {
            var destination = address.Queue;

            QueueClient sender;
            if (!senders.TryGetValue(destination, out sender))
            {
                lock (SenderLock)
                {
                    if (!senders.TryGetValue(destination, out sender))
                    {
                        try
                        {
                            sender = Factory.CreateQueueClient(destination);
                            senders[destination] = sender;
                        }
                        catch (MessagingEntityNotFoundException)
                        {
                            throw new QueueNotFoundException { Queue = Address.Parse(destination) };
                        }
                    }
                }
            }

            if (Transaction.Current == null)
                Send(message, sender);
            else
                Transaction.Current.EnlistVolatile(new SendResourceManager(() => Send(message, sender)), EnlistmentOptions.None);

        }

        private void Send(TransportMessage message, QueueClient sender)
        {
            var numRetries = 0;
            var sent = false;

            while (!sent)
            {
                try
                {
                    using (var brokeredMessage = message.Body != null ? new BrokeredMessage(message.Body) : new BrokeredMessage())
                    {
                        brokeredMessage.CorrelationId = message.CorrelationId;
                        if (message.TimeToBeReceived < TimeSpan.MaxValue) brokeredMessage.TimeToLive = message.TimeToBeReceived;
                        
                        foreach (var header in message.Headers)
                        {
                            brokeredMessage.Properties[header.Key] = header.Value;
                        }

                        brokeredMessage.Properties[Headers.MessageIntent] = message.MessageIntent.ToString();
                        brokeredMessage.MessageId = message.Id;
                        if ( message.ReplyToAddress != null )
                        {
                            brokeredMessage.ReplyTo = message.ReplyToAddress.ToString();
                        }
                        
                        sender.Send(brokeredMessage);
                        sent = true;
                    }
                }
                // todo: outbox
                catch (MessagingEntityDisabledException)
                {
                    numRetries++;

                    if (numRetries >= MaxDeliveryCount) throw;

                    Thread.Sleep(TimeSpan.FromSeconds(numRetries * DefaultBackoffTimeInSeconds));
                }
                    // back off when we're being throttled
                catch (ServerBusyException)
                {
                    numRetries++;

                    if (numRetries >= MaxDeliveryCount) throw;

                    Thread.Sleep(TimeSpan.FromSeconds(numRetries * DefaultBackoffTimeInSeconds));
                }
            }
        }

    }
}