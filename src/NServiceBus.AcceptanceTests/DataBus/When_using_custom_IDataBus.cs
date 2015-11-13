namespace NServiceBus.AcceptanceTests.DataBus
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.DataBus;
    using NUnit.Framework;

    public class When_using_custom_IDataBus : NServiceBusAcceptanceTest
    {
        static byte[] PayloadToSend = new byte[1024 * 10];

        [Test]
        public async Task Should_be_able_to_register_via_fluent()
        {
            var context = await Scenario.Define<Context>(c => { c.TempPath = Path.GetTempFileName(); })
                    .WithEndpoint<SenderViaFluent>(b => b.When(bus => bus.Send(new MyMessageWithLargePayload
                    {
                        Payload = new DataBusProperty<byte[]>(PayloadToSend)
                    })))
                    .WithEndpoint<ReceiverViaFluent>()
                    .Done(c => c.ReceivedPayload != null)
                    .Run();

            Assert.AreEqual(PayloadToSend, context.ReceivedPayload, "The large payload should be marshalled correctly using the databus");
        }

        public class Context : ScenarioContext
        {
            public string TempPath { get; set; }
            public byte[] ReceivedPayload { get; set; }
        }

        public class SenderViaFluent : EndpointConfigurationBuilder
        {
            public SenderViaFluent()
            {
                EndpointSetup<DefaultServer>(b => b.UseDataBus(typeof(MyDataBus)))
                    .AddMapping<MyMessageWithLargePayload>(typeof(ReceiverViaFluent));
            }
        }

        public class ReceiverViaFluent : EndpointConfigurationBuilder
        {
            public ReceiverViaFluent()
            {
                EndpointSetup<DefaultServer>(b => b.UseDataBus(typeof(MyDataBus)));
            }

            public class MyMessageHandler : IHandleMessages<MyMessageWithLargePayload>
            {
                public Context Context { get; set; }

                public Task Handle(MyMessageWithLargePayload messageWithLargePayload, IMessageHandlerContext context)
                {
                    Context.ReceivedPayload = messageWithLargePayload.Payload.Value;

                    return Task.FromResult(0);
                }
            }
        }

        public class MyDataBus : IDataBus
        {
            public Context Context { get; set; }

            public Task<Stream> Get(string key)
            {
                var fileStream = new FileStream(Context.TempPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
                return Task.FromResult((Stream) fileStream);
            }

            public Task<string> Put(Stream stream, TimeSpan timeToBeReceived)
            {
                using (var destination = File.OpenWrite(Context.TempPath))
                {
                    stream.CopyTo(destination);
                }
                return Task.FromResult("key");
            }

            public Task Start()
            {
                return Task.FromResult(0);
            }
        }

        [Serializable]
        public class MyMessageWithLargePayload : ICommand
        {
            public DataBusProperty<byte[]> Payload { get; set; }
        }
    }


}
