namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Metrics;

using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using Features;
using Newtonsoft.Json;
using NServiceBus.Routing;
using Transport;
using Conventions = AcceptanceTesting.Customization.Conventions;

public class When_payload_contains_multiple_logical_messages : OpenTelemetryAcceptanceTest
{
    class ProcessingEndpointWithMetrics : EndpointConfigurationBuilder
    {
        public ProcessingEndpointWithMetrics() => EndpointSetup<OpenTelemetryEnabledEndpoint>(c =>
        {
            c.RegisterStartupTask<ControlMessageSender>();
        });

        class ControlMessageSender : FeatureStartupTask
        {
            IMessageDispatcher dispatcher;

            public ControlMessageSender(IMessageDispatcher dispatcher)
            {
                this.dispatcher = dispatcher;
            }

            protected override async Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
            {

                var msgA = new MessageA();
                var msgB = new MessageB();
                var body = Encoding.Default.GetBytes($"[{JsonConvert.SerializeObject(msgA)},{JsonConvert.SerializeObject(msgB)}]");
                Dictionary<string, string> headers = new()
                {
                    { Headers.EnclosedMessageTypes, $"{msgA.GetType().FullName},{msgB.GetType().FullName}" }
                };
                var outgoingMessage = new OutgoingMessage("MyId", headers, body);
                var messageOperation = new TransportOperation(outgoingMessage, new UnicastAddressTag(Conventions.EndpointNamingConvention(typeof(ProcessingEndpointWithMetrics))));
                await dispatcher.Dispatch(new TransportOperations(messageOperation), new TransportTransaction(), cancellationToken);
            }

            protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }

    public class MessageA : IMessage
    {
    }

    public class MessageB : IMessage
    {
    }

}