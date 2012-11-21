namespace NServiceBus.Core.Tests.Transport
{
    using System.Threading;
    using NUnit.Framework;

    [TestFixture]
    public class When_specifying_a_non_zero_throughput_limit:for_the_transactional_transport
    {
        [Test]
        public void Should_limit_the_throughput_to_the_set_limit()
        {
            const int throughputLimit = 4;

            transport.MaxThroughputPerSecond = throughputLimit;
            transport.Start(Address.Parse("mytest"));

            ThreadPool.QueueUserWorkItem(Receive10);

            Thread.Sleep(600);
            
            Assert.AreEqual(throughputLimit, fakeReceiver.NumMessagesReceived);

            Thread.Sleep(600);
            Assert.AreEqual(throughputLimit * 2, fakeReceiver.NumMessagesReceived);
        }

        void Receive10(object state)
        {
            for (int i = 0; i < 10; i++)
            {
                fakeReceiver.FakeMessageReceived();

            }
        }
    }
}