//namespace NServiceBus.IntegrationTests.Automated.DataBus
//{
//    using System;
//    using EndpointTemplates;
//    using NUnit.Framework;
//    using ScenarioDescriptors;
//    using Support;

//    [TestFixture]
//    public class When_sending_databus_properties
//    {
//        [Test]
//        public void Should_receive_the_message_the_largeproperty_correctly()
//        {
//            var context = new ReceiveContext
//                {
//                    SentPayload = new byte[1024*10]
//                };

//            //Scenario.Define()
//            //        .WithEndpointBehaviour<SendBehavior>()
//            //        .WithEndpointBehaviour<ReceiveBehavior>(context)
//            //        .Run(r => r.For<AllTransports>().Except("activemq"))
//            //        .Should<ReceiveContext>(c=>
//            //            Assert.AreEqual(c.SentPayload,c.ReceivedPayload));



//        }

     
//        public class ReceiveContext : BehaviorContext
//        {
//            public byte[] SentPayload { get; set; }
//            public byte[] ReceivedPayload { get; set; }
//        }

//        public class SendBehavior : BehaviorFactory
//        {
//            public EndpointBehavior Get()
//            {
//                return new ScenarioBuilder("Sender")
//                    .EndpointSetup<DefaultServer>(c=>c.FileShareDataBus(@".\databus\sender"))
//                    .AddMapping<MyMessageWithLargePayload>("Receiver")
//                    .When(bus => bus.Send(new MyMessageWithLargePayload()))
//                    .CreateScenario();
//            }
//        }

//        public class ReceiveBehavior : BehaviorFactory
//        {
//            public EndpointBehavior Get()
//            {
//                return new ScenarioBuilder("Receiver")
//                    .EndpointSetup<DefaultServer>(c => c.FileShareDataBus(@".\databus\sender"))
//                    .Done((ReceiveContext context) => context.ReceivedPayload != null)
//                    .CreateScenario();
//            }
//        }

//        [Serializable]
//        public class MyMessageWithLargePayload : IMessage
//        {
//            public DataBusProperty<byte[]> Payload { get; set; }
//        }

//        public class MyMessageHandler : IHandleMessages<MyMessageWithLargePayload>
//        {
//            private readonly ReceiveContext context;

//            public MyMessageHandler(ReceiveContext context)
//            {
//                this.context = context;
//            }

//            public void Handle(MyMessageWithLargePayload messageWithLargePayload)
//            {
//                this.context.ReceivedPayload = messageWithLargePayload.Payload.Value;
//            }
//        }
//    }
//}
