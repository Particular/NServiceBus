// Remove unnecessary pragmas
#pragma warning disable IDE0079
// Type or member is obsolete
#pragma warning disable CS0618

namespace NServiceBus.AcceptanceTests.DataBus;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using MessageMutator;
using NUnit.Framework;

public class When_sending_databus_properties_from_different_environments : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_receive_messages_with_largepayload_correctly_from_windows()
    {
        var payloadToSend = new byte[PayloadSize];

        var context = await Scenario.Define<Context>()
            .WithEndpoint<WindowsSender>(b => b.When(session => session.Send(new MyMessageWithLargePayload
            {
                Payload = new DataBusProperty<byte[]>(payloadToSend)
            })))
            .WithEndpoint<Receiver>()
            .Done(c => c.ReceivedPayload != null)
            .Run();

        Assert.That(context.ReceivedPayload, Is.EqualTo(payloadToSend), "The large payload should be marshalled correctly using the databus");
    }

    [Test]
    public async Task Should_receive_messages_with_largepayload_correctly_from_linux()
    {
        var payloadToSend = new byte[PayloadSize];

        var context = await Scenario.Define<Context>()
            .WithEndpoint<LinuxSender>(b => b.When(session => session.Send(new MyMessageWithLargePayload
            {
                Payload = new DataBusProperty<byte[]>(payloadToSend)
            })))
            .WithEndpoint<Receiver>()
            .Done(c => c.ReceivedPayload != null)
            .Run();

        Assert.That(context.ReceivedPayload, Is.EqualTo(payloadToSend), "The large payload should be marshalled correctly using the databus");
    }

    const int PayloadSize = 500;

    public class Context : ScenarioContext
    {
        public byte[] ReceivedPayload { get; set; }
    }

    public class WindowsSender : EndpointConfigurationBuilder
    {
        public WindowsSender()
        {
            EndpointSetup<DefaultServer>(builder =>
            {
                var basePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "databus", "sender");
                builder.UseDataBus<FileShareDataBus, SystemJsonDataBusSerializer>().BasePath(basePath);
                builder.UseSerialization<SystemJsonSerializer>();
                builder.ConfigureRouting().RouteToEndpoint(typeof(MyMessageWithLargePayload), typeof(Receiver));
                builder.RegisterMessageMutator(new MutateOutgoingForWindows());
            });
        }

        public class MutateOutgoingForWindows : IMutateOutgoingTransportMessages
        {
            public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
            {
                var databusHeaderKey = context.OutgoingHeaders.FirstOrDefault(f => f.Key.StartsWith("NServiceBus.DataBus.", StringComparison.OrdinalIgnoreCase)).Key;
                context.OutgoingHeaders[databusHeaderKey] = context.OutgoingHeaders[databusHeaderKey].Replace("/", "\\");
                return Task.CompletedTask;
            }
        }
    }


    public class LinuxSender : EndpointConfigurationBuilder
    {
        public LinuxSender()
        {
            EndpointSetup<DefaultServer>(builder =>
            {
                var basePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "databus", "sender");
                builder.UseDataBus<FileShareDataBus, SystemJsonDataBusSerializer>().BasePath(basePath);
                builder.UseSerialization<SystemJsonSerializer>();
                builder.ConfigureRouting().RouteToEndpoint(typeof(MyMessageWithLargePayload), typeof(Receiver));
                builder.RegisterMessageMutator(new MutateOutgoingForLinux());
            });
        }

        public class MutateOutgoingForLinux : IMutateOutgoingTransportMessages
        {
            public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
            {
                var databusHeaderKey = context.OutgoingHeaders.FirstOrDefault(f => f.Key.StartsWith("NServiceBus.DataBus.", StringComparison.OrdinalIgnoreCase)).Key;
                context.OutgoingHeaders[databusHeaderKey] = context.OutgoingHeaders[databusHeaderKey].Replace("\\", "/");
                return Task.CompletedTask;
            }
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
                builder.UseSerialization<SystemJsonSerializer>();
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

                return Task.CompletedTask;
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
                return Task.CompletedTask;
            }
        }
    }

    public class MyMessageWithLargePayload : ICommand
    {
        public DataBusProperty<byte[]> Payload { get; set; }
    }
}
