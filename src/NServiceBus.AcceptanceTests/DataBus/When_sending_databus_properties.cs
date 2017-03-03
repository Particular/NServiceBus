﻿namespace NServiceBus.AcceptanceTests.DataBus
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using MessageMutator;
    using NUnit.Framework;

    public class When_sending_databus_properties : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_messages_with_largepayload_correctly()
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

        const int PayloadSize = 100;

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
                    var basePath = Path.Combine(TestContext.CurrentContext.TestDirectory, @"databus\sender");
                    builder.UseDataBus<FileShareDataBus>().BasePath(basePath);
                    builder.UseSerialization<JsonSerializer>();

                    builder.ConfigureTransport().Routing().RouteToEndpoint(typeof(MyMessageWithLargePayload), typeof(Receiver));
                });
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(builder =>
                {
                    var basePath = Path.Combine(TestContext.CurrentContext.TestDirectory, @"databus\sender");
                    builder.UseDataBus<FileShareDataBus>().BasePath(basePath);
                    builder.UseSerialization<JsonSerializer>();
                    builder.RegisterComponents(c => c.ConfigureComponent<Mutator>(DependencyLifecycle.InstancePerCall));
                });
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

            public class Mutator : IMutateIncomingTransportMessages
            {
                public Task MutateIncoming(MutateIncomingTransportMessageContext context)
                {
                    if (context.Body.Length > PayloadSize)
                    {
                        throw new Exception();
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