using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.ServiceModel;
using System.Transactions;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Unicast.Queuing.AppFabric
{
    public class AppFabricQueue : IReceiveMessages, ISendMessages
    {
        private readonly Dictionary<string, MessageSender> senders = new Dictionary<string, MessageSender>();
        private static readonly object SenderLock = new Object();

        private readonly MessagingFactory factory;
        private readonly ServiceBusNamespaceClient namespaceClient;
        private MessageReceiver receiver;
        private bool useTransactions;
        private QueueClient queueClient;
        private string queueName;

        public AppFabricQueue(MessagingFactory factory, ServiceBusNamespaceClient namespaceClient)
        {
            this.factory = factory;
            this.namespaceClient = namespaceClient;
        }


        public void Init(string address, bool transactional)
        {
            Init(Address.Parse(address), transactional);
        }

        public void Init(Address address, bool transactional)
        {
            try
            {
                queueName = address.Queue;
                var description = new QueueDescription {RequiresSession = false, RequiresDuplicateDetection = false, MaxQueueSizeInBytes = 104857600};
                namespaceClient.CreateQueue(queueName, description);
            }
            catch (MessagingEntityAlreadyExistsException)
            {
                // the queue already exists, which is ok
            }

            queueClient = factory.CreateQueueClient(queueName);
            receiver = queueClient.CreateReceiver(ReceiveMode.PeekLock);
            receiver.Faulted += (o, args) => receiver = queueClient.CreateReceiver(ReceiveMode.PeekLock);
           
            useTransactions = transactional;
        }

      

        public bool HasMessage()
        {
            return true;
        }

        public TransportMessage Receive()
        {
            BrokeredMessage message;
            if(receiver.TryReceive(out message))
            {
                var rawMessage = message.GetBody<byte[]>();
                var t = DeserializeMessage(rawMessage);

                if (!useTransactions || Transaction.Current == null)
                {
                    try
                    {
                        message.Complete();
                    }
                    catch (MessageLockLostException)
                    {
                        // message has been completed by another thread or worker
                    }
                }
                else
                    Transaction.Current.EnlistVolatile(new ReceiveResourceManager(message), EnlistmentOptions.None);

                return t;
            }
            return null;
        }

        public void Send(TransportMessage message, string destination)
        {
            Send(message, Address.Parse(destination));
        }

        public void Send(TransportMessage message, Address address)
        {
            var destination = address.Queue;
                
            MessageSender sender;
            if(!senders.TryGetValue(destination, out sender) || sender.State == CommunicationState.Faulted)
            {
                lock (SenderLock)
                {
                    if (!senders.TryGetValue(destination, out sender) || sender.State == CommunicationState.Faulted)
                    {
                            try
                            {
                                var c = factory.CreateQueueClient(destination);
                                sender = c.CreateSender();
                                senders[destination] = sender;
                            }
                            catch (MessagingEntityNotFoundException)
                            {
                                throw new QueueNotFoundException { Queue = destination };
                            }
                    }
                }
            }

            message.Id = Guid.NewGuid().ToString();
            var rawMessage = SerializeMessage(message);

            var brokeredMessage = BrokeredMessage.CreateMessage(rawMessage);

            if (Transaction.Current == null)
                sender.Send(brokeredMessage);
            else
                Transaction.Current.EnlistVolatile(new SendResourceManager(sender, brokeredMessage), EnlistmentOptions.None);
           
        }

        private static byte[] SerializeMessage(TransportMessage originalMessage)
        {
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, originalMessage);
                return stream.ToArray();
            }


        }

        private static TransportMessage DeserializeMessage(byte[] rawMessage)
        {
            var formatter = new BinaryFormatter();

            using (var stream = new MemoryStream(rawMessage))
            {
                var message = formatter.Deserialize(stream) as TransportMessage;

                if (message == null)
                    throw new SerializationException("Failed to deserialize message");

                return message;
            }
        }
    }
}
