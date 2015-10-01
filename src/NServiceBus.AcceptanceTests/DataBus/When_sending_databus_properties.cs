namespace NServiceBus.AcceptanceTests.DataBus
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_sending_databus_properties : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_messages_with_largepayload_correctly()
        {
            var payloadToSend = new byte[1024 * 1024 * 10];

            var context = await Scenario.Define<Context>()
                    .WithEndpoint<Sender>(b => b.When(bus => bus.SendAsync(new MyMessageWithLargePayload
                    {
                        Payload = new DataBusProperty<byte[]>(payloadToSend)
                    })))
                    .WithEndpoint<Receiver>()
                    .Done(c => c.ReceivedPayload != null)
                    .Run();

            Assert.AreEqual(payloadToSend, context.ReceivedPayload, "The large payload should be marshalled correctly using the databus");
        }

        public class Context : ScenarioContext
        {
            public byte[] ReceivedPayload { get; set; }
        }


        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(builder => builder.UseDataBus<FileShareDataBus>().BasePath(@".\databus\sender"))
                    .AddMapping<MyMessageWithLargePayload>(typeof(Receiver));
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(builder => builder.UseDataBus<FileShareDataBus>().BasePath(@".\databus\sender"));
            }

            public class MyMessageHandler : IHandleMessages<MyMessageWithLargePayload>
            {
                public Context Context { get; set; }

                public Task Handle(MyMessageWithLargePayload messageWithLargePayload)
                {
                    Context.ReceivedPayload = messageWithLargePayload.Payload.Value;

                    return Task.FromResult(0);
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
