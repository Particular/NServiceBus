using NUnit.Framework;

namespace NServiceBus.Unicast.Queuing.Azure.Tests
{
    [TestFixture]
    [Category("Azure")]
    public class When_initializing_the_queue : AzureQueueFixture
    {
        [Test]
        public void A_purge_can_be_requested()
        {
            AddTestMessage();
            AddTestMessage();
            AddTestMessage();

            receiver.PurgeOnStartup = true;

            receiver.Init(QueueName, false);

            Assert.Null(receiver.Receive());
        }
    }
} 