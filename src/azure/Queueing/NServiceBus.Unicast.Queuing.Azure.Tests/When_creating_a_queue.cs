using NUnit.Framework;

namespace NServiceBus.Unicast.Queuing.Azure.Tests
{
    [TestFixture]
    [Category("Azure")]
    public class When_creating_a_queue : AzureQueueFixture
    {
        [Test]
        public void A_native_azure_queue_should_be_created()
        {
            receiver.CreateQueue(QueueName);

            Assert.True(nativeQueue.Exists());
        }
    }
} 