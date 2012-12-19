namespace NServiceBus.Gateway.Tests.Idempotency.Raven
{
    using System.Collections.Generic;
    using System.IO;
    using System.Messaging;
    using System.Threading;
    using System.Transactions;
    using NUnit.Framework;
    using Persistence.Raven;

    [TestFixture]
    public class When_acking_an_existing_message : in_the_raven_storage
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
                ravenPersister.AckMessage(message.ClientId, out msg, out headers);
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

            ravenPersister.AckMessage(message.ClientId, out msg, out headers);
            
            Assert.True(GetStoredMessage(message.ClientId).Acknowledged);
        }

        [Test, Category("Integration")]
        public void Raven_dtc_bug()
        {
            new MessageQueue(QueueAddress, QueueAccessMode.ReceiveAndAdmin)
            .Purge();

            using (var tx = new TransactionScope())
            {
                
                using (var session = store.OpenSession())
                {
                    session.Store(new GatewayMessage());
                    session.SaveChanges();
                }

                using (var q = new MessageQueue(QueueAddress, QueueAccessMode.Send))
                {
                    var toSend = new Message { BodyStream = new MemoryStream(new byte[8]) };

                    //sending a message to a msmq queue causes raven to promote the tx
                    q.Send(toSend, MessageQueueTransactionType.Automatic);
                }

                //when we complete raven commits it tx but the DTC tx is never commited and eventually times out
                tx.Complete();
            }
            Thread.Sleep(1000);

            Assert.AreEqual(1,new MessageQueue(QueueAddress, QueueAccessMode.ReceiveAndAdmin)
            .GetAllMessages().Length);
        }
    }
}