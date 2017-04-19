namespace NServiceBus.Core.Tests.Msmq
{
    using System.Messaging;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
    using MessageType = Unicast.Subscriptions.MessageType;

    public class MsmqSubscriptionStorageIntegrationTests
    {
        [TearDown]
        public void TearDown()
        {
            DeleteQueueIfPresent("MsmqSubscriptionStorageQueueTests.PersistTransactional");
            DeleteQueueIfPresent("MsmqSubscriptionStorageQueueTests.PersistNonTransactional");
        }

        [Test]
        public async Task ShouldRemoveSubscriptionsInTransactionalMode()
        {
            var address = MsmqAddress.Parse("MsmqSubscriptionStorageQueueTests.PersistTransactional");
            var queuePath = address.PathWithoutPrefix;

            if (MessageQueue.Exists(queuePath))
            {
                MessageQueue.Delete(queuePath);
            }

            MessageQueue.Create(queuePath, true);

            using (var queue = new MessageQueue(queuePath))
            {
                queue.Send(new Message
                {
                    Label = "subscriber",
                    Body = typeof(MyMessage).AssemblyQualifiedName
                }, MessageQueueTransactionType.Single);
            }

            var storage = new MsmqSubscriptionStorage(new MsmqSubscriptionStorageQueue(address, true));

            storage.Init();

            await storage.Unsubscribe(new Subscriber("subscriber", "subscriber"), new MessageType(typeof(MyMessage)), new ContextBag());

            using (var queue = new MessageQueue(queuePath))
            {
                CollectionAssert.IsEmpty(queue.GetAllMessages());
            }
        }

        [Test]
        public async Task ShouldRemoveSubscriptionsInNonTransactionalMode()
        {
            var address = MsmqAddress.Parse("MsmqSubscriptionStorageQueueTests.PersistNonTransactional");
            var queuePath = address.PathWithoutPrefix;

            if (MessageQueue.Exists(queuePath))
            {
                MessageQueue.Delete(queuePath);
            }

            MessageQueue.Create(queuePath, false);

            using (var queue = new MessageQueue(queuePath))
            {
                queue.Send(new Message
                {
                    Label = "subscriber",
                    Body = typeof(MyMessage).AssemblyQualifiedName
                }, MessageQueueTransactionType.None);
            }

            var storage = new MsmqSubscriptionStorage(new MsmqSubscriptionStorageQueue(address, false));

            storage.Init();

            await storage.Unsubscribe(new Subscriber("subscriber", "subscriber"), new MessageType(typeof(MyMessage)), new ContextBag());

            using (var queue = new MessageQueue(queuePath))
            {
                CollectionAssert.IsEmpty(queue.GetAllMessages());
            }
        }

        void DeleteQueueIfPresent(string queueName)
        {
            var path = MsmqAddress.Parse(queueName).PathWithoutPrefix;

            if (MessageQueue.Exists(path))
            {
                MessageQueue.Delete(path);
            }
        }

        class MyMessage
        {
        }
    }
}