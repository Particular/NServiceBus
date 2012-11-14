namespace NMSConsumer
{
    using System;
    using System.Transactions;

    using Apache.NMS;
    using Apache.NMS.ActiveMQ;
    using Apache.NMS.Util;

    using MyMessages;

    class Program
    {
        static void Main(string[] args)
        {
            var connectionFactory = new NetTxConnectionFactory("activemq:tcp://localhost:61616")
                {
                    AcknowledgementMode = AcknowledgementMode.Transactional,
                    PrefetchPolicy = { QueuePrefetch = 1, TopicPrefetch = 1, DurableTopicPrefetch = 1 }
                };

            using (var connection = connectionFactory.CreateNetTxConnection())
            {
                connection.Start();
                using (var session = connection.CreateNetTxSession())
                {
                    var destination = SessionUtil.GetDestination(session, "queue://Consumer.NMS.VirtualTopic.EventMessage");
                    using (var consumer = session.CreateConsumer(destination))
                    {
                        consumer.Listener += OnMessage;
                        Console.WriteLine("Consumer started. Press q to quit");
                        while (Console.ReadKey().KeyChar != 'q')
                        {
                        }
                    }
                }
                connection.Stop();
            }
        }

        private static void OnMessage(IMessage message)
        {
            using (new TransactionScope())
            {
                var textMessage = (ITextMessage)message;
                var eventMessage = (EventMessage)XmlUtil.Deserialize(typeof(EventMessage), textMessage.Text);

                Console.WriteLine("Received Message with ID: " + eventMessage.EventId);
            }
        }
    }
}
