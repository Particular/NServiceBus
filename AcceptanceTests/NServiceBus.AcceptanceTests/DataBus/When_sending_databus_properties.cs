namespace NServiceBus.AcceptanceTests.DataBus
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_sending_databus_properties:NServiceBusAcceptanceTest
    {
        static byte[] PayloadToSend = new byte[1024 * 1024 * 10];

        [Test]
        public void Should_receive_the_message_the_largeproperty_correctly()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<Sender>(b => b.Given(bus=> bus.Send(new MyMessageWithLargePayload
                        {
                            Payload = new DataBusProperty<byte[]>(PayloadToSend) 
                        })))
                    .WithEndpoint<Receiver>()
                    .Done(context => context.ReceivedPayload != null)
                    .Repeat(r => r.For<AllTransports>()
                                  .For<AllSerializers>())
                    .Should(c => Assert.AreEqual(PayloadToSend, c.ReceivedPayload, "The large payload should be marshalled correctly using the databus"))
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public byte[] ReceivedPayload { get; set; }
        }


        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c => c.FileShareDataBus(@".\databus\sender"))
                    .AddMapping<MyMessageWithLargePayload>(typeof (Receiver));
            }
        }

        public class Receiver : EndpointConfigurationBuilder
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
