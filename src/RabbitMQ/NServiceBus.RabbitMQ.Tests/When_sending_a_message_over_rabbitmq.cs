namespace NServiceBus.Transport.RabbitMQ.Tests
{
    using System;
    using System.Text;
    using NUnit.Framework;
    using Unicast.Queuing;
    using global::RabbitMQ.Client;
    using global::RabbitMQ.Client.Events;


    [TestFixture, Category("Integration")]
    public class When_sending_a_message_over_rabbitmq : RabbitMqContext
    {
        const string TESTQUEUE = "testendpoint";

        [Test]
        public void Should_populate_the_body()
        {
            var body = Encoding.UTF8.GetBytes("<TestMessage/>");

            Verify(new TransportMessageBuilder().WithBody(body),
                 received => Assert.AreEqual(body, received.Body));
        }


        [Test]
        public void Should_set_the_content_type()
        {
            VerifyRabbit(new TransportMessageBuilder().WithHeader(Headers.ContentType, "application/json"),
                received => Assert.AreEqual("application/json", received.BasicProperties.ContentType ));
 
        }

        [Test]
        public void Should_set_the_time_to_be_received()
        {

            var timeToBeReceived = TimeSpan.FromDays(1);


            VerifyRabbit(new TransportMessageBuilder().TimeToBeReceived(timeToBeReceived),
                received => Assert.AreEqual(timeToBeReceived.TotalMilliseconds.ToString(), received.BasicProperties.Expiration));
        }

        [Test]
        public void Should_set_the_reply_to_address()
        {
            var address = Address.Parse("myAddress");

            Verify(new TransportMessageBuilder().ReplyToAddress(address),
                (t, r) =>
                    {
                        Assert.AreEqual(address, t.ReplyToAddress);
                        Assert.AreEqual(address.Queue, r.BasicProperties.ReplyTo);
                    });

        }

        [Test]
        public void Should_set_the_message_intent()
        {
            Verify(new TransportMessageBuilder().Intent(MessageIntentEnum.Publish),
                result =>Assert.AreEqual(MessageIntentEnum.Publish, result.MessageIntent)
                );

        }



        [Test]
        public void Should_set_correlation_id_if_present()
        {
            var correlationId = Guid.NewGuid().ToString();

            Verify(new TransportMessageBuilder().CorrelationId(correlationId),
                result => Assert.AreEqual(correlationId, result.CorrelationId));

        }

        [Test]
        public void Should_transmitt_all_transportmessage_headers()
        {

            Verify(new TransportMessageBuilder().WithHeader("h1", "v1").WithHeader("h2", "v2"),
                result =>
                    {
                        Assert.AreEqual("v1",result.Headers["h1"]);
                        Assert.AreEqual("v2", result.Headers["h2"]);
                    });

        }

        [Test, Ignore("Not sure we should enforce this")]
        public void Should_throw_when_sending_to_a_nonexisting_queue()
        {
            Assert.Throws<QueueNotFoundException>(() =>
                 sender.Send(new TransportMessage
                 {

                 }, Address.Parse("NonExistingQueue@localhost")));
        }

        void Verify(TransportMessageBuilder builder, Action<TransportMessage, BasicDeliverEventArgs> assertion)
        {
            var message = builder.Build();

            SendMessage(message);

            var result = Consume(message.Id);

            assertion(RabbitMqTransportMessageExtensions.ToTransportMessage(result), result);
        }
        void Verify(TransportMessageBuilder builder, Action< TransportMessage> assertion)
        {
            Verify(builder,(t,r)=>assertion(t));           
        }

        void VerifyRabbit(TransportMessageBuilder builder, Action<BasicDeliverEventArgs> assertion)
        {
            Verify(builder, (t, r) => assertion(r));
        }


        
        void SendMessage(TransportMessage message)
        {
            MakeSureQueueExists(TESTQUEUE);

            sender.Send(message, Address.Parse("TestEndpoint@localhost"));
        }

        BasicDeliverEventArgs Consume(string id)
        {

            using (var channel = connection.CreateModel())
            {
                var consumer = new QueueingBasicConsumer(channel);

                channel.BasicConsume(TESTQUEUE, true, consumer);

                object message;

                if (!consumer.Queue.Dequeue(1000, out message))
                    throw new InvalidOperationException("No message found in queue");

                var e = (BasicDeliverEventArgs)message;

                if (e.BasicProperties.MessageId != id)
                    throw new InvalidOperationException("Unexpected message found in queue");

                return e;
            }
        }





    }
}