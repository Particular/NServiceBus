using System;
using System.Collections.Generic;
using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
using NServiceBus.Serializers.Binary;
using NServiceBus.Serializers.XML;
using NUnit.Framework;

namespace NServiceBus.Unicast.Queuing.SQLServer.Tests
{
    [TestFixture, Ignore]
    public class SqlServerMessageQueueTests
    {
        private SqlServerMessageQueue _mq;
        private XmlMessageSerializer _xmlMessageSerializer;
        private MessageMapper _messageMapper;
        private MessageSerializer _binMessageSerializer;
        private SimpleMessageMapper _simpleMessageMapper;

        [SetUp]
        public void SetUp()
        {
            var listOfTypes = new List<Type> {typeof (object)};

            _simpleMessageMapper = new SimpleMessageMapper();
            _binMessageSerializer = new MessageSerializer();            

            _messageMapper = new MessageMapper();
            _messageMapper.Initialize(listOfTypes);

            _xmlMessageSerializer = new XmlMessageSerializer(_messageMapper);
            _xmlMessageSerializer.Initialize(listOfTypes);

            _mq = new SqlServerMessageQueue
                      {
                          ConnectionString = "Server=MIKNOR8540WW7\\sqlexpress;Database=NSB;Trusted_Connection=True;",
                          MessageSerializer = _xmlMessageSerializer
                      };
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
        public void HasMessage()
        {
            Init();
            _mq.HasMessage();
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
            if (_mq.HasMessage())
            {
                _mq.Receive();
                received = true;
            }
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
            while (_mq.HasMessage())                                       
            {
                _mq.Receive();
            }
            stopwatch.Stop();
            Console.WriteLine("stopwatch: " + stopwatch.Elapsed);
        }

        [Test]
        public void All100()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            for(int i = 0; i < 1000; i++)
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

            for(int i = 0; i < 50000; i++)
            {
                Send();
            }

            TimeSpan send = stopwatch.Elapsed;

            Console.WriteLine("send: " + stopwatch.Elapsed);

            while (_mq.HasMessage())
            {
                _mq.Receive();
            }

            stopwatch.Stop();
            Console.WriteLine("receive: " + stopwatch.Elapsed.Subtract(send));
            Console.WriteLine("total: " + stopwatch.Elapsed);
        }

        static TransportMessage CreateNewTransportMessage()
        {
            var message = new TransportMessage
                              {
                                  Id = "Id",
                                  Body = new byte[100],
                                  CorrelationId = "CorrelationId",
                                  Recoverable = true,
                                  ReplyToAddress = Address.Parse("replyto@address"),
                                  TimeToBeReceived = TimeSpan.FromMinutes(1),
                                  Headers = new Dictionary<string, string>(),
                                  MessageIntent = MessageIntentEnum.Send,
                                  TimeSent = DateTime.UtcNow,
                                  IdForCorrelation = "IdForCorrelation"
                              };
            return message;
        }
    }
}
