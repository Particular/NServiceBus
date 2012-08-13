using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
using NServiceBus.Serializers.XML;
using NUnit.Framework;
using NServiceBus.Config;

namespace NServiceBus.Unicast.Queuing.SQLServer.Tests
{
    [TestFixture, Ignore]
    public class SqlServerMessageQueueTests
    {
        private SqlServerMessageQueue _mq;
        private XmlMessageSerializer _xmlMessageSerializer;
        private MessageMapper _messageMapper;

        private const string ConStr = "Server=MIKNOR8540WW7\\sqlexpress;Database=NSB;Trusted_Connection=True;";

        [SetUp]
        public void SetUp()
        {
            var listOfTypes = new List<Type> {typeof (object)};

            _messageMapper = new MessageMapper();
            _messageMapper.Initialize(listOfTypes);

            _xmlMessageSerializer = new XmlMessageSerializer(_messageMapper);
            _xmlMessageSerializer.Initialize(listOfTypes);

            _mq = new SqlServerMessageQueue
                      {
                          ConnectionString = ConStr,
                          MessageSerializer = _xmlMessageSerializer
                      };

            var creator = new SqlServerQueueCreator {ConnectionString = ConStr};
            creator.CreateQueueIfNecessary(_address, "test");
        }

        private readonly Address _address = new Address("send", "testhost");

        [Test]
        public void Init()
        {
            _mq.Init(_address, true);            
        }

        [Test]
        public void Send()
        {
            _mq.Send(CreateNewTransportMessage(), _address);
        }

        [Test]
        public void Receive()
        {
            Init();
            Send();
            _mq.Receive();
        }

        [Test]
        public void All()
        {
            bool received = false;

            Init();
            Send();
            _mq.Receive();
            received = true;
            Assert.That(received, Is.True);
        }

        [Test]
        public void SendMany()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 10000; i++)
            {
                Send();
            }
            stopwatch.Stop();
            Console.WriteLine("stopwatch: " + stopwatch.Elapsed);
        }

        [Test]
        public void ReceiveMany()
        {   
            Init();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
            {
                _mq.Receive();
            }
            stopwatch.Stop();
            Console.WriteLine("stopwatch: " + stopwatch.Elapsed);
        }

        [Test]
        public void ReceiveAll()
        {
            Init();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                _mq.Receive();
            stopwatch.Stop();
            Console.WriteLine("stopwatch: " + stopwatch.Elapsed);
        }

        [Test]
        public void All100()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            for(int i = 0; i < 100; i++)
            {
                All();
            }
            stopwatch.Stop();
            Console.WriteLine("stopwatch: " + stopwatch.Elapsed);
        }

        [Test]
        public void MultipleSendAndLaterReceive()
        {
            Init();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            for(int i = 0; i < 2000; i++)
            {
                Send();
            }

            TimeSpan send = stopwatch.Elapsed;

            Console.WriteLine("send: " + stopwatch.Elapsed);

            bool moreMessages = true;
            var threads = new Thread[10];

            for (int i = 0; i < threads.Length; i++ )
            {
                bool more = moreMessages;
                threads[i] = new Thread(x => { while (more) { moreMessages = _mq.Receive() != null; } });
            }
           
            foreach (var thread in threads)
            {
                thread.Start();
            }

            while (moreMessages) { }

            stopwatch.Stop();
            Console.WriteLine("receive: " + stopwatch.Elapsed.Subtract(send));
            Console.WriteLine("total: " + stopwatch.Elapsed);
        }

        [Test]
        public void AreEqual()
        {
            Init();

            var m1 = CreateNewTransportMessage();
            _mq.Send(m1, _address);
            var m2 = _mq.Receive();

            Assert.AreEqual(m1.Body, m2.Body);
            Assert.AreEqual(m1.CorrelationId, m2.CorrelationId);
            Assert.AreEqual(m1.Headers, m2.Headers);
            Assert.AreEqual(m1.Id, m2.Id);
            Assert.AreEqual(m1.IdForCorrelation, m2.IdForCorrelation);
            Assert.AreEqual(m1.MessageIntent, m2.MessageIntent);
            Assert.AreEqual(m1.Recoverable, m2.Recoverable);
            Assert.AreEqual(m1.ReplyToAddress, m2.ReplyToAddress);
            Assert.AreEqual(m1.TimeToBeReceived, m2.TimeToBeReceived);

        }

        static TransportMessage CreateNewTransportMessage()
        {
            var message = new TransportMessage
                              {
                                  Id = Guid.NewGuid().ToString(),
                                  Body = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()) ,
                                  CorrelationId = Guid.NewGuid().ToString(),
                                  Recoverable = true,
                                  ReplyToAddress = Address.Parse("replyto@address"),
                                  TimeToBeReceived = TimeSpan.FromMinutes(1),
                                  Headers = new Dictionary<string, string>(),
                                  MessageIntent = MessageIntentEnum.Send,
                                  IdForCorrelation = Guid.NewGuid().ToString()
                              };
            
            message.Headers.Add("TimeSent",DateTime.UtcNow.ToString());

            return message;
        }
    }
}
