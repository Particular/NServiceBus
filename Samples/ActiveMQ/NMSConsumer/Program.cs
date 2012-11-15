namespace NMSConsumer
{
    using System;
    using System.Transactions;

    using Apache.NMS;
    using Apache.NMS.ActiveMQ;
    using Apache.NMS.Util;

    using MyMessages;
    using MyMessages.SubscriberNMS;

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
                    var eventDestination = SessionUtil.GetDestination(session, "queue://Consumer.NMS.VirtualTopic.EventMessage");
                    var commandDestination = SessionUtil.GetDestination(session, "queue://subscribernms");
                    using (var eventConsumer = session.CreateConsumer(eventDestination))
                    using (var commandConsumer = session.CreateConsumer(commandDestination))
                    {
                        eventConsumer.Listener += OnEventMessage;
                        commandConsumer.Listener += OnCommandMessage;
                        
                        Console.WriteLine("Consumer started. Press q to quit");
                        while (Console.ReadKey().KeyChar != 'q')
                        {
                        }
                    }
                }
                connection.Stop();
            }
        }

        private static void OnEventMessage(IMessage message)
        {
            using (new TransactionScope())
            {
                var textMessage = (ITextMessage)message;
                var messageContent = (EventMessage)XmlUtil.Deserialize(typeof(EventMessage), textMessage.Text);

                Console.WriteLine("Received EventMessage with ID: {0}", messageContent.EventId);
            }
        }

        private static void OnCommandMessage(IMessage message)
        {
            using (new TransactionScope())
            {
                var textMessage = (ITextMessage)message;
                var messageContent = (MyRequestNMS)XmlUtil.Deserialize(typeof(MyRequestNMS), textMessage.Text);

                Console.WriteLine("Received MyRequestNMS with ID: {0}", messageContent.EventId);
            }
        }
    }
}
