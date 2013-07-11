using System.Transactions;
using NUnit.Framework;

namespace NServiceBus.Unicast.Queuing.Azure.Tests
{
    using Microsoft.WindowsAzure.Storage.Queue;

    [TestFixture]
    [Category("Azure")]
    public class When_sending_messages : AzureQueueFixture
    {
        const string destinationQueueName = "destination";

        [Test]
        public void Should_not_appear_at_destination_if_transaction_rollbacks()
        {
            var destinationQueue = GetDestinationQueue();
            using (new TransactionScope())
            {
                sender.Send(new TransportMessage(), destinationQueueName);
            }
            Assert.Null(destinationQueue.GetMessage());
        }

        private CloudQueue GetDestinationQueue()
        {
            var destinationQueue = client.GetQueueReference(destinationQueueName);

            destinationQueue.CreateIfNotExists();
            destinationQueue.Clear();

            return destinationQueue;
        }

        [Test]
        public void The_message_should_appear_in_the_destination_queue()
        {
            var destinationQueue = GetDestinationQueue();

            sender.Send(new TransportMessage(), destinationQueueName);

            Assert.NotNull(destinationQueue.GetMessage());
        }

        [Test]
        public void The_message_id_should_be_updated_with_the_native_id()
        {
            GetDestinationQueue();
            var message = new TransportMessage();

            sender.Send(message, destinationQueueName);

            Assert.NotNull(message.Id);
        }
        [Test]
        public void A_QueueNotFoundException_should_be_thrown_if_the_desitnation_queue_does_not_exists()
        {
            Assert.Throws<QueueNotFoundException>(() => sender.Send(new TransportMessage(), "whatever"));
        }
    }
}