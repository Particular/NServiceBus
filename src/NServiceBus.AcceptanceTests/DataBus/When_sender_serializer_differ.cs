namespace NServiceBus.AcceptanceTests.DataBus
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using MessageMutator;
    using NUnit.Framework;

    public class When_sender_serializer_differ : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_deserialize_based_on_serializer_used()
        {
            var payloadToSend = new byte[PayloadSize];

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(b => b.When(session => session.Send(new MyMessageWithLargePayload
                {
                    Payload = new DataBusProperty<byte[]>(payloadToSend)
                })))
                .WithEndpoint<Receiver>()
                .Done(c => c.ReceivedPayload != null)
                .Run();

            Assert.AreEqual(payloadToSend, context.ReceivedPayload, "The large payload should be marshalled correctly using the databus");
        }

        const int PayloadSize = 500;

        public class Context : ScenarioContext
        {
            public byte[] ReceivedPayload { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(builder =>
                {
                    var basePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "databus", "sender");
#pragma warning disable CS0618
                    builder.UseDataBus<FileShareDataBus, BinaryFormatterDataBusSerializer>().BasePath(basePath);
#pragma warning restore CS0618

                    builder.ConfigureRouting().RouteToEndpoint(typeof(MyMessageWithLargePayload), typeof(Receiver));
                });
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(builder =>
                {
                    var basePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "databus", "sender");
                    builder.UseDataBus<FileShareDataBus, SystemJsonDataBusSerializer>().BasePath(basePath);
                    builder.RegisterMessageMutator(new Mutator());
                });
            }

            public class MyMessageHandler : IHandleMessages<MyMessageWithLargePayload>
            {
                public MyMessageHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MyMessageWithLargePayload messageWithLargePayload, IMessageHandlerContext context)
                {
                    testContext.ReceivedPayload = messageWithLargePayload.Payload.Value;

                    return Task.FromResult(0);
                }

                Context testContext;
            }

            public class Mutator : IMutateIncomingTransportMessages
            {
                public Task MutateIncoming(MutateIncomingTransportMessageContext context)
                {
                    if (context.Body.Length > PayloadSize)
                    {
                        throw new Exception("The message body is too large, which means the DataBus was not used to transfer the payload.");
                    }
                    return Task.FromResult(0);
                }
            }
        }

        public class MyMessageWithLargePayload : ICommand
        {
            public DataBusProperty<byte[]> Payload { get; set; }
        }
    }
}