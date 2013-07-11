using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Transactions;
using NUnit.Framework;

namespace NServiceBus.Unicast.Queuing.Azure.Tests
{
    using Microsoft.WindowsAzure.Storage.Queue;

    [TestFixture]
    [Category("Azure")]
    public class When_receiving_messages : AzureQueueFixture
    {
       

        [Test]
        public void Should_throw_if_non_nservicebus_messages_are_received()
        {
            nativeQueue.AddMessage(new CloudQueueMessage("whatever"));

            Assert.Throws<SerializationException>(() => receiver.Receive());
        }

        [Test]
        public void Should_default_to_non_transactionable_if_no_ambient_transaction_exists()
        {
            AddTestMessage();
            receiver.MessageInvisibleTime = 1;

            Assert.NotNull(receiver.Receive());
            Thread.Sleep(1000);
            Assert.Null(receiver.Receive());
        }

        [Test]
        public void Messages_should_not_reapper_in_the_queue_if_transaction_is_committed()
        {
            AddTestMessage();

            receiver.MessageInvisibleTime = 1;
            using (var scope = new TransactionScope())
            {
                Assert.NotNull(receiver.Receive());
          
                scope.Complete();
            }

            Thread.Sleep(1000);

            Assert.Null(receiver.Receive());
        }

        [Test]
        public void The_received_message_should_reappear_in_the_queue_if_transaction_is_not_comitted()
        {
            AddTestMessage();

            receiver.MessageInvisibleTime = 2;
            using (new TransactionScope())
            {
                Assert.NotNull(receiver.Receive());

                //rollback
            }
            Thread.Sleep(1000);

            Assert.NotNull(receiver.Receive());
        }

        [Test]
        public void Received_messages_should_be_removed_from_the_queue()
        {
            AddTestMessage();

            receiver.MessageInvisibleTime = 1;

            receiver.Receive();

            Thread.Sleep(1000);

            Assert.Null(receiver.Receive());
        }

        [Test]
        public void Send_messages_without_body_should_be_ok()
        {
            AddTestMessage();

            var message = receiver.Receive();

            Assert.Null(message.Body);
        }

        [Test]
        public void All_properties_should_be_preserved()
        {
            var formatter = new BinaryFormatter();

            using (var stream = new MemoryStream())
            {
                var testMessage = new TestMessage {TestProperty = "Test"};
                formatter.Serialize(stream,testMessage);

                var original = new TransportMessage
                                   {
                                       Body = stream.ToArray(),
                                       MessageIntent = MessageIntentEnum.Send,
                                       CorrelationId = "123",
                                       //Id = "11111",
                                       Recoverable = true,
                                       ReplyToAddress= Address.Parse("response"),
                                       TimeToBeReceived = TimeSpan.FromHours(1)
                                   };
                AddTestMessage(original);

                var result = receiver.Receive();

                var resultMessage = formatter.Deserialize(new MemoryStream(result.Body)) as TestMessage;
                Assert.AreEqual(resultMessage.TestProperty,"Test");


                Assert.AreEqual( result.MessageIntent,original.MessageIntent);
                Assert.AreEqual(result.CorrelationId,original.CorrelationId);
                Assert.NotNull(result.Id);
                Assert.AreEqual(result.Recoverable,original.Recoverable);
                Assert.AreEqual(result.ReplyToAddress,original.ReplyToAddress);
                Assert.AreEqual(result.TimeToBeReceived,original.TimeToBeReceived);

            }
        }
    }

    [Serializable]
    public class TestMessage
    {
        public string TestProperty { get; set; }
    }
}