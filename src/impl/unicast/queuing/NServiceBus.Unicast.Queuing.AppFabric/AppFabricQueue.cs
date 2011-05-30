using System;
using System.Collections.Generic;
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

        public AppFabricQueue(MessagingFactory factory, ServiceBusNamespaceClient namespaceClient)
        {
            this.factory = factory;
            this.namespaceClient = namespaceClient;
        }

        public void Init(string address, bool transactional)
        {
            try
            {
                var description = new QueueDescription {RequiresSession = false, RequiresDuplicateDetection = false, MaxQueueSizeInBytes = 104857600};
                namespaceClient.CreateQueue(address, description);
            }
            catch (MessagingEntityAlreadyExistsException)
            {
                // the queue already exists, which is ok
            }
            
            queueClient = factory.CreateQueueClient(address);
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

            try
            {
                if(receiver.TryReceive(out message))
                {
                    var t = message.GetBody<TransportMessage>();

                    if (!useTransactions || Transaction.Current == null)
                        message.Complete();
                    else
                        Transaction.Current.EnlistVolatile(new ReceiveResourceManager(message), EnlistmentOptions.None);

                    return t;
                }
            }
            catch (MessageLockLostException)
            {
                // message has been completed by another thread or worker
            }
            catch (Exception)
            {
                throw;
            }
            

            return null;
        }

        public void Send(TransportMessage message, string destination)
        {
            try
            {
                MessageSender sender;
                if(!senders.TryGetValue(destination, out sender) || sender.State == CommunicationState.Faulted)
                {
                    lock (SenderLock)
                    {
                        if (!senders.TryGetValue(destination, out sender) || sender.State == CommunicationState.Faulted)
                        {
                            var queueClient = factory.CreateQueueClient(destination);
                            sender = queueClient.CreateSender();
                            senders[destination] = sender;
                        }
                    }
                }

                message.Id = Guid.NewGuid().ToString();

                var brokeredMessage = BrokeredMessage.CreateMessage(message);

                if (Transaction.Current == null)
                    sender.Send(brokeredMessage);
                else
                    Transaction.Current.EnlistVolatile(new SendResourceManager(sender, brokeredMessage), EnlistmentOptions.None);

            }
            catch (MessagingEntityNotFoundException)
            {
                throw new QueueNotFoundException{ Queue = destination };
            }
            catch(Exception)
            {
                throw;
            }

        }
    }
}
