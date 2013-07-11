namespace NMSConsumer
{
    using System;
    using System.IO;
    using System.Transactions;
    using System.Xml;
    using System.Xml.Serialization;

    using Apache.NMS;
    using Apache.NMS.ActiveMQ;
    using Apache.NMS.Util;

    using MyMessages;
    using MyMessages.Publisher;
    using MyMessages.SubscriberNMS;

    class Program
    {
        private static INetTxConnection currentconnection;
        private static int nextResponseType = 0;

        private INetTxSession session;

        static void Main(string[] args)
        {
            new Program().Run();
        }

        private void Run()
        {
            var connectionFactory = new NetTxConnectionFactory("failover:(tcp://localhost:61616,tcp://localhost:61616)?randomize=false&timeout=5000")
                {
                    AcknowledgementMode = AcknowledgementMode.Transactional,
                    PrefetchPolicy = { QueuePrefetch = 1 }
                };

            using (var connection = connectionFactory.CreateNetTxConnection())
            {
                currentconnection = connection;
                connection.Start();
                using (var session = connection.CreateNetTxSession())
                {
                    this.session = session;
                    var eventDestination = SessionUtil.GetDestination(session, "queue://Consumer.NMS.VirtualTopic.EventMessage");
                    var commandDestination = SessionUtil.GetDestination(session, "queue://subscribernms");
                    using (var eventConsumer = session.CreateConsumer(eventDestination))
                    using (var commandConsumer = session.CreateConsumer(commandDestination))
                    {
                        eventConsumer.Listener += OnEventMessage;
                        commandConsumer.Listener += this.OnCommandMessage;

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
                var messageContent = (EventMessage)Deserialize(typeof(EventMessage), textMessage.Text);

                Console.WriteLine("Received EventMessage with ID: {0}", messageContent.EventId);
            }
        }

        private void OnCommandMessage(IMessage message)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required))
            {
                var textMessage = (ITextMessage)message;
                var text = textMessage.Text;
                var messageContent = (MyRequestNMS)Deserialize(typeof(MyRequestNMS), text);

                Console.WriteLine("Received MyRequestNMS with ID: {0}", messageContent.CommandId);

                try
                {
           //         using (var session = currentconnection.CreateNetTxSession())
                    {
                        using (var producer = this.session.CreateProducer())
                        {
                            var responseMessage = GetResponseMessage(this.session, textMessage);
                            var destination = message.NMSReplyTo;

                            producer.Send(destination, responseMessage);
                            Console.WriteLine("Sent response: "  + responseMessage.Properties["ErrorCode"]);
                        }
                    }
                }
                catch (Exception)
                {
                }

                if (messageContent.ThrowExceptionDuringProcessing)
                {
                    Console.WriteLine("Throwing Exception");
                    throw new Exception();
                }

                scope.Complete();
            }
        }

        public static object Deserialize(Type objType, string text)
        {
            if (text == null)
            {
                return null;
            }
            try
            {
                XmlSerializer serializer = new XmlSerializer(objType);
                return serializer.Deserialize(new NamespaceIgnorantXmlTextReader(new StringReader(text)));
            }
            catch (Exception ex)
            {
                Tracer.ErrorFormat("Error deserializing object: {0}", new object[] { ex.Message });
                return null;
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

    public class NamespaceIgnorantXmlTextReader : XmlTextReader
    {
        public NamespaceIgnorantXmlTextReader(System.IO.TextReader reader) : base(reader) { }

        public override string NamespaceURI
        {
            get { return ""; }
        }
    }
}
