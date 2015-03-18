namespace NServiceBus.Core.Tests.Transport
{
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Unicast.Transport;
    using NUnit.Framework;

    [TestFixture, Explicit("Timing sensitive")]
    public class ThroughputLimiterTests
    {
        const int ThroughputLimit = 4;

        [Test]
        public void Should_limit_the_throughput_to_the_set_limit()
        {
            var limiter = new ThroughputLimiter();
            limiter.Start(ThroughputLimit);
            var counter = 0;

            Task.Factory.StartNew(() =>
            {
                for (var i = 0; i < ThroughputLimit*2; i++)
                {
                    limiter.MessageProcessed();
                    counter++;
                }

            });

            Thread.Sleep(600);
            Assert.AreEqual(ThroughputLimit, counter);

            Thread.Sleep(500);
            Assert.AreEqual(ThroughputLimit * 2, counter);
        }

    }
}