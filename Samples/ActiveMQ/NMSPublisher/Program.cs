using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMSPublisher
{
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
                    var destination = SessionUtil.GetDestination(session, "topic://VirtualTopic.EventMessage");
                    using (var producer = session.CreateProducer(destination))
                    {
                        Console.WriteLine("Publisher started. Press q to quit");
                        while (Console.ReadKey().KeyChar != 'q')
                        {
                            var message =
                                session.CreateXmlMessage(
                                    new EventMessage()
                                        {
                                            Time = DateTime.Now,
                                            Duration = TimeSpan.FromMinutes(5),
                                            EventId = Guid.NewGuid()
                                        });
                            producer.Send(destination, message);
                        }
                    }
                }
                connection.Stop();
            }
        }
    }
}
