namespace NServiceBus.Core.Tests.Transport
{
    using NUnit.Framework;

    [TestFixture]
    public class When_specifying_a_zero_throughput_limit : for_the_transactional_transport
    {
        [Test]
        public void Should_not_limit_the_throughput()
        {
            const int throughputLimit = 0;

            TransportReceiver.ChangeMaximumMessageThroughputPerSecond(throughputLimit);
            TransportReceiver.Start(Address.Parse("mytest"));

            for (var i = 0; i < 100; i++)
            {
                fakeReceiver.FakeMessageReceived();

            }
            Assert.AreEqual(100, fakeReceiver.NumberOfMessagesReceived);
        }
    }
}