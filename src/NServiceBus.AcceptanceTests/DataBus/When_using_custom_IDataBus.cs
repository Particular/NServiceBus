namespace NServiceBus.AcceptanceTests.DataBus
{
    using System;
    using System.IO;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.DataBus;
    using NUnit.Framework;

    public class When_using_custom_IDataBus : NServiceBusAcceptanceTest
    {
        static byte[] PayloadToSend = new byte[1024 * 10];

        [Test]
        public void Should_be_able_to_register_via_fluent()
        {
            var context = new Context
            {
                TempPath = Path.GetTempFileName()
            };

            Scenario.Define(context)
                    .WithEndpoint<SenderViaFluent>(b => b.Given(bus => bus.Send(new MyMessageWithLargePayload
                    {
                        Payload = new DataBusProperty<byte[]>(PayloadToSend)
                    })))
                    .WithEndpoint<ReceiverViaFluent>()
                    .Done(c => c.ReceivedPayload != null)
                    .Run();

            Assert.AreEqual(PayloadToSend, context.ReceivedPayload, "The large payload should be marshalled correctly using the databus");
        }

        [Test]
        public void Should_be_able_to_register_via_container()
        {
            var context = new Context
            {
                TempPath = Path.GetTempFileName()
            };

            Scenario.Define(context)
                    .WithEndpoint<Sender>(b => b.Given(bus=> bus.Send(new MyMessageWithLargePayload
                        {
                            Payload = new DataBusProperty<byte[]>(PayloadToSend) 
                        })))
                    .WithEndpoint<Receiver>()
                    .Done(c => c.ReceivedPayload != null)
                    .Run();

            Assert.AreEqual(PayloadToSend, context.ReceivedPayload, "The large payload should be marshalled correctly using the databus");
        }

        public class Context : ScenarioContext
        {
            public string TempPath { get; set; }
            public byte[] ReceivedPayload { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(b => b.RegisterComponents(r => r.RegisterSingleton<IDataBus>(new MyDataBus())))
                    .AddMapping<MyMessageWithLargePayload>(typeof(Receiver));
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(b => b.RegisterComponents(r => r.RegisterSingleton<IDataBus>(new MyDataBus())));
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

                public void Handle(MyMessageWithLargePayload messageWithLargePayload)
                {
                    Context.ReceivedPayload = messageWithLargePayload.Payload.Value;
                }
            }
        }

        public class MyDataBus : IDataBus
        {
            public Context Context { get; set; }

            public Stream Get(string key)
            {
                return File.OpenRead(Context.TempPath);
            }

            public string Put(Stream stream, TimeSpan timeToBeReceived)
            {
                using (var destination = File.OpenWrite(Context.TempPath))
                {
                    stream.CopyTo(destination);
                }
                return "key";
            }

            public void Start()
            {
            }
        }

        [Serializable]
        public class MyMessageWithLargePayload : ICommand
        {
            public DataBusProperty<byte[]> Payload { get; set; }
        }
    }

   
}
