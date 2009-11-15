using System.Transactions;
using Microsoft.Samples.ServiceHosting.StorageClient;
using NBehave.Spec.NUnit;
using NServiceBus.Unicast.Queuing;
using NUnit.Framework;

namespace NServiceBus.Unicast.Queueing.Azure.Tests
{
    [TestFixture]
    public class When_sending_messages : AzureQueueFixture
    {
        const string destinationQueueName = "destination";

        [Test]
        public void Should_not_appear_at_destination_if_transaction_rollbacks()
        {
            var destinationQueue = GetDestinationQueue();
            using (new TransactionScope())
            {
                queue.Send(new QueuedMessage(), destinationQueueName, true);
            }
            destinationQueue.GetMessage().ShouldBeNull();
        }

        private MessageQueue GetDestinationQueue()
        {
            var destinationQueue = storage.GetQueue(destinationQueueName);

            destinationQueue.CreateQueue();
            destinationQueue.Clear();

            return destinationQueue;
        }

        [Test]
        public void The_message_should_appear_in_the_destination_queue()
        {
            var destinationQueue = GetDestinationQueue();

            queue.Send(new QueuedMessage(), destinationQueueName, false);

            destinationQueue.GetMessage().ShouldNotBeNull();
        }

        [Test]
        public void The_message_id_should_be_updated_with_the_native_id()
        {
            GetDestinationQueue();
            var message = new QueuedMessage();

            queue.Send(message, destinationQueueName, false);

            message.Id.ShouldNotBeNull();
        }
        [Test]
        public void A_QueueNotFoundException_should_be_thrown_if_the_desitnation_queue_does_not_exists()
        {
            Assert.Throws<QueueNotFoundException>(() => queue.Send(new QueuedMessage(), "whatever", true));
        }
    }
}