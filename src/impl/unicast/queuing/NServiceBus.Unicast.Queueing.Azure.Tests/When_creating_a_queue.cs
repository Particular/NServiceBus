using NUnit.Framework;

namespace NServiceBus.Unicast.Queueing.Azure.Tests
{
    [TestFixture]
    public class When_creating_a_queue:AzureQueueFixture
    {
        [Test]
        public void A_native_azure_queue_should_be_created()
        { 
            queue.CreateQueue(QueueName);

            Assert.True(nativeQueue.DoesQueueExist());
        }
    }
} 