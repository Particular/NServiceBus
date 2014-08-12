namespace NServiceBus.AcceptanceTests.DataBus
{
    using System;
    using System.IO;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.DataBus;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_sending_with_custom_IDataBus : NServiceBusAcceptanceTest
    {
        static byte[] PayloadToSend = new byte[1024 * 10];

        [Test]
        public void Should_receive_the_message_the_correctly()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<Sender>(b => b.Given(bus=> bus.Send(new MyMessageWithLargePayload
                        {
                            Payload = new DataBusProperty<byte[]>(PayloadToSend) 
                        })))
                    .WithEndpoint<Receiver>()
                    .Done(context => context.ReceivedPayload != null)
                    .Repeat(r => r.For<AllSerializers>())
                    .Should(c => Assert.AreEqual(PayloadToSend, c.ReceivedPayload, "The large payload should be marshalled correctly using the databus"))
                    .Run();
            File.Delete(MyDataBus.GetTempPath());
        }

        public class Context : ScenarioContext
        {
            public byte[] ReceivedPayload { get; set; }
        }


        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c => { }, b => b.RegisterComponents(r => r.RegisterSingleton<IDataBus>(new MyDataBus())))
                    .AddMapping<MyMessageWithLargePayload>(typeof(Receiver));
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c => { }, b => b.RegisterComponents(r => r.RegisterSingleton<IDataBus>(new MyDataBus())));
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

    public class MyDataBus:IDataBus
    {
        string tempPath;

        public MyDataBus()
        {
            tempPath = GetTempPath();
        }

        public static string GetTempPath()
        {
            return Path.Combine(Path.GetTempPath(), "MyDataBus.txt");
        }

        public Stream Get(string key)
        {
            return File.OpenRead(tempPath);
        }

        public string Put(Stream stream, TimeSpan timeToBeReceived)
        {
            using (var destination = File.OpenWrite(tempPath))
            {
                stream.CopyTo(destination);
            }
            return "key";
        }

        public void Start()
        {
        }
    }
}
