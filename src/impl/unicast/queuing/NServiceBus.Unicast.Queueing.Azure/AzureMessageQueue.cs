using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Transactions;
using Microsoft.Samples.ServiceHosting.StorageClient;
using NServiceBus.Unicast.Queuing;

namespace NServiceBus.Unicast.Queueing.Azure
{
    public class AzureMessageQueue : IMessageQueue
    {
        private MessageQueue queue;
        private readonly QueueStorage storage;
        private int secondsToWait;
        
        public int MessageInvisibleTime { get; set; }

        public AzureMessageQueue(QueueStorage storage)
        {
            this.storage = storage;
            MessageInvisibleTime = 30;
        }

        public void Init(string inputqueue, bool purge, int secondsToWaitForMessage)
        {
            secondsToWait = secondsToWaitForMessage;

            queue = storage.GetQueue(inputqueue);

            if (purge)
                queue.Clear();
        }

        public void Send(QueuedMessage message, string destination, bool transactional)
        {
            var sendQueue = storage.GetQueue(destination);

            if (!sendQueue.DoesQueueExist())
                throw new QueueNotFoundException();


            message.Id = Guid.NewGuid().ToString();

            var rawMessage = SerializeMessage(message);

            if (!transactional || Transaction.Current == null)
                sendQueue.PutMessage(rawMessage);
            else
                Transaction.Current.EnlistVolatile(new SendResourceManager(sendQueue, rawMessage), EnlistmentOptions.None);

            
        }

        public bool HasMessage()
        {
            return queue.PeekMessage() != null;
        }

        public QueuedMessage Receive(bool transactional)
        {
            var rawMessage = GetMessage(transactional);

            if (rawMessage == null)
                return null;

            return DeserializeMessage(rawMessage);
        }

        private Message GetMessage(bool transactional)
        {
            var receivedMessage = PollForMessage();

            if (receivedMessage != null)
            {
                if (!transactional || Transaction.Current == null)
                    queue.DeleteMessage(receivedMessage);
                else
                    Transaction.Current.EnlistVolatile(new ReceiveResourceManager(queue, receivedMessage),EnlistmentOptions.None);
            }

            return receivedMessage;
        }

        private Message PollForMessage()
        {
            Message message;
            var maxTime = DateTime.Now.AddSeconds(secondsToWait);


            while ((message = queue.GetMessage(MessageInvisibleTime)) == null && DateTime.Now < maxTime)
            {
                Thread.Sleep(500);
            }

            return message;
        }

        private static Message SerializeMessage(QueuedMessage originalMessage)
        {
            return new AzureMessage(originalMessage).ToNativeMessage();
        }

        private static QueuedMessage DeserializeMessage(Message rawMessage)
        {
            var formatter = new BinaryFormatter();

            byte[] data = rawMessage.ContentAsBytes();

            using (var stream = new MemoryStream(data))
            {
                var message = formatter.Deserialize(stream) as AzureMessage;

                if (message == null)
                    throw new SerializationException("Failed to deserialize message with id: " + rawMessage.Id);

                return message.ToQueueMessage();
            }
        }

        public void CreateQueue(string queueName)
        {
            storage.GetQueue(queueName)
                .CreateQueue();
        }
    }
}