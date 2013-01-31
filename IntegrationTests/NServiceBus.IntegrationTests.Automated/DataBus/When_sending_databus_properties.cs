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
        static byte[] PayloadToSend = new byte[1024 * 1024 * 10];
        [Test]
        public void Should_receive_the_message_the_largeproperty_correctly()
        {
             Scenario.Define()
                    .WithEndpoint<Sender>()
                    .WithEndpoint<Receiver>(new Context())
                    .Done<Context>(context => context.ReceivedPayload != null)
                    .Repeat(r => r.For<AllTransports>()
                                  .For<AllSerializers>())
                    .Should<Context>(c => Assert.AreEqual(PayloadToSend, c.ReceivedPayload, "The large payload should be marshalled correctly using the databus"))
                    .Run();
        }

        public class Context : BehaviorContext
        {
            public byte[] ReceivedPayload { get; set; }
        }


        public class Sender : EndpointBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c => c.FileShareDataBus(@".\databus\sender"))
                    .AddMapping<MyMessageWithLargePayload>(typeof(Receiver))
                    .When(bus => bus.Send(new MyMessageWithLargePayload
                        {
                            Payload = new DataBusProperty<byte[]>(PayloadToSend) 
                        }));
            }
        }

        public class Receiver : EndpointBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c => c.FileShareDataBus(@".\databus\sender"));
            }

            public class MyMessageHandler : IHandleMessages<MyMessageWithLargePayload>
            {
                public Context Context { get; set; }

                public void Handle(MyMessageWithLargePayload messageWithLargePayload)
                {
                    Context.ReceivedPayload = messageWithLargePayload.Payload.Value;
                }
            }
        }

        [Serializable]
        public class MyMessageWithLargePayload : ICommand
        {
            public DataBusProperty<byte[]> Payload { get; set; }
        }
    }
}
