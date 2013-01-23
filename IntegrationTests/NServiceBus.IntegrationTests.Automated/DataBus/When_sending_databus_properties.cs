namespace NServiceBus.IntegrationTests.Automated.DataBus
{
    using System;
    using EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;
    using Support;

    [TestFixture]
    public class When_sending_databus_properties:NServiceBusIntegrationTest
    {
        [Test]
        public void Should_receive_the_message_the_largeproperty_correctly()
        {
            var payloadToSend = new byte[1024*1024*10];

            Scenario.Define()
                    .WithEndpoint<Sender>(new SendContext{PayloadToSend = payloadToSend})
                    .WithEndpoint<Receiver>(new ReceiveContext())
                    .Repeat(r => 
                                r.For<AllTransports>().Except(Transports.ActiveMQ)
                                .For<AllSerializers>()
                            )
                    .Should<ReceiveContext>(c => Assert.AreEqual(payloadToSend, c.ReceivedPayload))
                    .Run();
        }


        public class ReceiveContext : BehaviorContext
        {
            public byte[] ReceivedPayload { get; set; }
        }

        public class SendContext : BehaviorContext
        {
            public byte[] PayloadToSend { get; set; }
        }

        public class Sender : EndpointBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c => c.FileShareDataBus(@".\databus\sender"))
                    .AddMapping<MyMessageWithLargePayload>(typeof(Receiver))
                    .When<SendContext>((bus,context) => bus.Send(new MyMessageWithLargePayload
                        {
                            Payload = new DataBusProperty<byte[]>(context.PayloadToSend) 
                        }));
            }
        }

        public class Receiver : EndpointBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c => c.FileShareDataBus(@".\databus\sender"))
                    .Done<ReceiveContext>(context => context.ReceivedPayload != null);
            }

            public class MyMessageHandler : IHandleMessages<MyMessageWithLargePayload>
            {
                public ReceiveContext Context { get; set; }

                public void Handle(MyMessageWithLargePayload messageWithLargePayload)
                {
                    Context.ReceivedPayload = messageWithLargePayload.Payload.Value;

                    Context.ReceivedPayload[5] = 3;
                }
            }
        }

        [Serializable]
        public class MyMessageWithLargePayload : IMessage
        {
            public DataBusProperty<byte[]> Payload { get; set; }
        }
    }
}
