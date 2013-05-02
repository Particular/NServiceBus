using System;
using System.Collections.Generic;
using System.IO;
using System.Transactions;
using Microsoft.WindowsAzure;
using NServiceBus.Serialization;

namespace NServiceBus.Unicast.Queuing.Azure
{
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Transports;

    /// <summary>
    /// 
    /// </summary>
    public class AzureMessageQueueSender : ISendMessages
    {
        private readonly Dictionary<string, CloudQueueClient> destinationQueueClients = new Dictionary<string, CloudQueueClient>();
        private static readonly object SenderLock = new Object();
        /// <summary>
        /// Gets or sets the message serializer
        /// </summary>
        public IMessageSerializer MessageSerializer { get; set; }

        public CloudQueueClient Client { get; set; }

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
            var sendClient = GetClientForConnectionString(address.Machine) ?? Client;

            var sendQueue = sendClient.GetQueueReference(SanitizeQueueName(address.Queue));

            if (!sendQueue.Exists())
                throw new QueueNotFoundException();

            if (string.IsNullOrEmpty(message.Id)) message.Id = Guid.NewGuid().ToString();

            var rawMessage = SerializeMessage(message);

            if (Transaction.Current == null)
            {
                sendQueue.AddMessage(rawMessage);
            }
            else
                Transaction.Current.EnlistVolatile(new SendResourceManager(sendQueue, rawMessage), EnlistmentOptions.None);
        }

        private CloudQueueClient GetClientForConnectionString(string connectionString)
        {
            CloudQueueClient sendClient;

            if (!destinationQueueClients.TryGetValue(connectionString, out sendClient))
            {
                lock (SenderLock)
                {
                    if (!destinationQueueClients.TryGetValue(connectionString, out sendClient))
                    {
                        CloudStorageAccount account;

                        if (CloudStorageAccount.TryParse(connectionString, out account))
                        {
                            sendClient = account.CreateCloudQueueClient();
                        }

                        // sendClient could be null, this is intentional 
                        // so that it remembers a connectionstring was invald 
                        // and doesn't try to parse it again.

                        destinationQueueClients.Add(connectionString, sendClient);
                    }
                }
            }

            return sendClient;
        }

        private CloudQueueMessage SerializeMessage(TransportMessage message)
        {
            using (var stream = new MemoryStream())
            {
                var toSend = new MessageWrapper
                    {
                        Id = message.Id,
                        Body = message.Body,
                        CorrelationId = message.CorrelationId,
                        Recoverable = message.Recoverable,
                        ReplyToAddress = message.ReplyToAddress == null ? Address.Self.ToString() : message.ReplyToAddress.ToString(),
                        TimeToBeReceived = message.TimeToBeReceived,
                        Headers = message.Headers,
                        MessageIntent = message.MessageIntent
                    };


                MessageSerializer.Serialize(new IMessage[] { toSend }, stream);
                return new CloudQueueMessage(stream.ToArray());
            }
        }

        public void CreateQueue(string queueName)
        {
            Client.GetQueueReference(SanitizeQueueName(queueName)).CreateIfNotExists();
        }

        private string SanitizeQueueName(string queueName)
        {
            // The auto queue name generation uses namespaces which includes dots, 
            // yet dots are not supported in azure storage names
            // that's why we replace them here.
            // and make sure there are no upper cases

            return queueName.Replace('.', '-').ToLowerInvariant();
        }
    }
}