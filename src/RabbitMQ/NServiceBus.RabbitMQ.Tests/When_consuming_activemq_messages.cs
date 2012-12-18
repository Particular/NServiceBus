namespace NServiceBus.RabbitMQ.Tests
{
    using System.Threading;
    using NUnit.Framework;
    using Unicast.Transport.Transactional;

    [TestFixture, Explicit("Integration tests")]
    public class When_consuming_activemq_messages:RabbitMqContext
    {
        [SetUp]
        public void SetUp()
        {
            MakeSureQueueExists(TESTQUEUE);

            dequeueStrategy = new RabbitMqDequeueStrategy()
            {
                Connection = connection,
            };
        }

        [TearDown]
        public void TearDown()
        {
            dequeueStrategy.Stop();
        }

        [Test]
        public void Should_block_until_a_message_is_available()
        {
            var address = Address.Parse(TESTQUEUE);

            var sr = new ManualResetEvent(false);
            TransportMessage received = null;
            
            dequeueStrategy.TryProcessMessage+= (m) =>
                {
                    received = m;
                    sr.Set();
                    return true;
                };

            dequeueStrategy.Init(address,new TransactionSettings{IsTransactional = true});
            dequeueStrategy.Start(1);

            var message = new TransportMessage();

            sender.Send(message, address);

            sr.WaitOne();

            Assert.AreEqual(message.Id,received.Id);
        }

        RabbitMqDequeueStrategy dequeueStrategy;

        const string TESTQUEUE = "testreceiver";
    }
}