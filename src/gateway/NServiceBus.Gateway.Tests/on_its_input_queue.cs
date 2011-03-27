namespace NServiceBus.Gateway.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using MessageInterfaces.MessageMapper.Reflection;
    using NUnit.Framework;
    using Rhino.Mocks;
    using Serializers.XML;
    using Unicast.Queuing;
    using Unicast.Queuing.Msmq;
    using Unicast.Transport;

    public class on_its_input_queue
    {

        protected ISendMessages msmqSender;
        protected HttpListener listener;
        protected ISendMessages testSender;
        protected IPersistMessages idempotencyEnforcer;
        protected IMessageNotifier notifier;

        IBus bus;

        [SetUp]
        public void SetUp()
        {
            msmqSender = new MsmqMessageSender();
            testSender = MockRepository.GenerateStub<ISendMessages>();
            notifier = MockRepository.GenerateStub<IMessageNotifier>();

            idempotencyEnforcer = MockRepository.GenerateStub<IPersistMessages>();


            listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8092/Gateway/");

            listener.Start();


            bus = Configure.With()
                .DefaultBuilder()
                .XmlSerializer()
                .FileShareDataBus("./databus")
                .InMemoryFaultManagement()
                .UnicastBus()
                .MsmqTransport()
                .CreateBus()
                .Start();
        }


        protected void SendMessageToGatewayQueue(IMessage messageToSend)
        {
            bus.Send("gateway",messageToSend);

            var context = listener.GetContext();

            var handler = new HttpReceiver(testSender, notifier, "", "", idempotencyEnforcer);

            //handle first send
            handler.Handle(context);


            //handle ack
            context = listener.GetContext();
            handler.Handle(context);
        }


        [TearDown]
        public void TearDown()
        {
            listener.Stop();
        }
    }
}