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

    public class When_sending_databus_properties_with_unobtrusive : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_messages_with_largepayload_correctly()
        {
            var payloadToSend = new byte[PayloadSize];

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(b => b.When(session => session.Send(new MyMessageWithLargePayload
                {
                    Payload = payloadToSend
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
                    builder.Conventions()
                        .DefiningCommandsAs(t => t.Namespace != null && t.FullName == typeof(MyMessageWithLargePayload).FullName)
                        .DefiningDataBusPropertiesAs(t => t.Name.Contains("Payload"));

                    var basePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "databus", "sender");
                    builder.UseDataBus<FileShareDataBus, SystemJsonDataBusSerializer>().BasePath(basePath);

                    builder.ConfigureRouting().RouteToEndpoint(typeof(MyMessageWithLargePayload), typeof(Receiver));
                }).ExcludeType<MyMessageWithLargePayload>(); // remove that type from assembly scanning to simulate what would happen with true unobtrusive mode
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(builder =>
                {
                    builder.Conventions()
                        .DefiningCommandsAs(t => t.Namespace != null && t.FullName == typeof(MyMessageWithLargePayload).FullName)
                        .DefiningDataBusPropertiesAs(t => t.Name.Contains("Payload"));

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
                    testContext.ReceivedPayload = messageWithLargePayload.Payload;

                    return Task.FromResult(0);
                }

                Context testContext;
            }
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

        public class MyMessageWithLargePayload
        {
            public byte[] Payload { get; set; }
        }
    }
}