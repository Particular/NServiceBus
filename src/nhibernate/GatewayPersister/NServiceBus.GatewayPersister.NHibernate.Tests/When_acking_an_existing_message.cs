namespace NServiceBus.GatewayPersister.NHibernate.Tests
{
    using System.Collections.Generic;
    using System.Transactions;
    using NUnit.Framework;

    [TestFixture]
    public class When_acking_an_existing_message : BaseStorage
    {
        const string QueueAddress = "FormatName:DIRECT=OS:win2008r2\\private$\\headquarter.gateway";
          

        [Test]
        public void Should_return_the_message_and_headers()
        {
            var message = CreateTestMessage();

            Store(message);

            byte[] msg;

            IDictionary<string, string> headers;

            using (var tx = new TransactionScope())
            {
                Persister.AckMessage(message.ClientId, out msg, out headers);
                tx.Complete();
            }

            Assert.AreEqual(message.Headers, headers);
            Assert.AreEqual(message.OriginalMessage, msg);
        }

        [Test]
        public void Should_mark_the_message_as_acked()
        {
            var message = CreateTestMessage();

            Store(message);

            byte[] msg;

            IDictionary<string, string> headers;

            Persister.AckMessage(message.ClientId, out msg, out headers);
            
            Assert.True(GetStoredMessage(message.ClientId).Acknowledged);
        }
    }
}