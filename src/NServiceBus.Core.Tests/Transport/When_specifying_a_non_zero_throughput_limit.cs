namespace NServiceBus.Core.Tests.Transport
{
    using System.Threading;
    using NUnit.Framework;

    [TestFixture,Explicit("Timing sensitive")]
    public class When_specifying_a_non_zero_throughput_limit : for_the_transactional_transport
    {
        const int ThroughputLimit = 4;

        [Test]
        public void Should_limit_the_throughput_to_the_set_limit()
        {
            TransportReceiver.ChangeMaximumMessageThroughputPerSecond(ThroughputLimit);
            TransportReceiver.Start(Address.Parse("mytest"));

            ThreadPool.QueueUserWorkItem(Receive10);

            Thread.Sleep(600);
            Assert.AreEqual(ThroughputLimit, fakeReceiver.NumberOfMessagesReceived);

            Thread.Sleep(500);
            Assert.AreEqual(ThroughputLimit * 2, fakeReceiver.NumberOfMessagesReceived);
        }

        private void Receive10(object state)
        {
            for (var i = 0; i < ThroughputLimit*2; i++)
            {
                fakeReceiver.FakeMessageReceived();
            }
        }
    }
}