using NUnit.Framework;

namespace NServiceBus.Unicast.Queuing.Azure.Tests
{
    [TestFixture]
    public class When_initializing_the_queue:AzureQueueFixture
    {
     
        [Test]
        public void A_purge_can_be_requested()
        {
            AddTestMessage();
            AddTestMessage();
            AddTestMessage();

            queue.PurgeOnStartup = true;

            queue.Init(QueueName,false);

            Assert.Null(queue.Receive());
        }
    }
} 