using NBehave.Spec.NUnit;
using NUnit.Framework;

namespace NServiceBus.Unicast.Queueing.Azure.Tests
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

            queue.Init(QueueName,true,1);


            queue.Receive(false).ShouldBeNull();
        }
    }
} 