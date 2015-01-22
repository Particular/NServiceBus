namespace NServiceBus.Unicast.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class TransportMessageTests
    {
        [Test]
        public void Should_set_the_time_sent_header()
        {
            var message = new TransportMessage();

            Assert.True(message.Headers.ContainsKey(Headers.TimeSent));
        }
    }
}