namespace NMSConsumer
{
    using System;
    using System.Transactions;

    using Apache.NMS;
    using Apache.NMS.ActiveMQ;
    using Apache.NMS.ActiveMQ.Commands;
    using Apache.NMS.Util;

    using MyMessages;
    using MyMessages.Publisher;
    using MyMessages.SubscriberNMS;

    class Program
    {
        private static INetTxConnection currentconnection;
        private static int nextResponseType = 0;

        static void Main(string[] args)
        {
            var connectionFactory = new NetTxConnectionFactory("activemq:tcp://localhost:61616")
                {
                    AcknowledgementMode = AcknowledgementMode.Transactional,
                    PrefetchPolicy = { QueuePrefetch = 1 }
                };

            using (var connection = connectionFactory.CreateNetTxConnection())
            {
                currentconnection = connection;
                connection.Start();
                using (var session = connection.CreateSession(AcknowledgementMode.Transactional))
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
            //using (var scope = new TransactionScope(TransactionScopeOption.Required))
            {
                var textMessage = (ITextMessage)message;
                var messageContent = (MyRequestNMS)XmlUtil.Deserialize(typeof(MyRequestNMS), textMessage.Text);

                Console.WriteLine("Received MyRequestNMS with ID: {0}", messageContent.CommandId);

                try
                {
                    using (var session = currentconnection.CreateNetTxSession())
                    {
                        using (var producer = session.CreateProducer())
                        {
                            var responseMessage = GetResponseMessage(session, textMessage);
                            var destinationAddress = message.NMSReplyTo.ToString();
                            var destination =
                                new ActiveMQTempQueue(destinationAddress.Substring("temp-queue://".Length));

                            producer.Send(destination, responseMessage);
                        }
                    }
                }
                catch (Exception e)
                {
                }

                //scope.Complete();
            }
        }

        private static IMessage GetResponseMessage(INetTxSession session, ITextMessage textMessage)
        {
            IMessage responseMessage;

            if (nextResponseType == 0)
            {
                var errorCode = new Random().Next(2) == 1 ? ResponseCode.Ok : ResponseCode.Failed;

                responseMessage = session.CreateTextMessage("");
                responseMessage.Properties["ErrorCode"] = (int)errorCode;
                nextResponseType = 1;
            }
            else
            {
                var messageContent = new ResponseToPublisher
                    {
                        ResponseId = Guid.NewGuid(),
                        Time = DateTime.Now.Second > -1 ? (DateTime?)DateTime.Now : null,
                        Duration = TimeSpan.FromSeconds(99999D),
                    };

                responseMessage = session.CreateXmlMessage(messageContent);
                nextResponseType = 0;
            }

            responseMessage.NMSCorrelationID = textMessage.NMSMessageId;
            return responseMessage;
        }
    }
}
