namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.Pipeline;
using NServiceBus.Transport;
using NUnit.Framework;

public class When_incoming_message_has_no_trace : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_start_new_trace()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<ReceivingEndpoint>(b => b
                .When(s => s.SendLocal(new IncomingMessage()))) // tracing headers are removed from message
            .Done(c => c.MessageReceived)
            .Run();

        var incomingMessageActivity = NServicebusActivityListener.CompletedActivities.GetReceiveMessageActivities().Single();
        Assert.That(incomingMessageActivity.ParentId, Is.EqualTo(null), "should start a trace when incoming message isn't part of a trace already");
    }

    class Context : ScenarioContext
    {
        public bool MessageReceived { get; set; }
    }

    class ReceivingEndpoint : EndpointConfigurationBuilder
    {
        public ReceivingEndpoint() => EndpointSetup<OpenTelemetryEnabledEndpoint>(c => c
            .Pipeline.Register(new StopTraceBehavior(), "removes tracing headers from outgoing messages"));

        class StopTraceBehavior : Behavior<IDispatchContext>
        {
            public override Task Invoke(IDispatchContext context, Func<Task> next)
            {
                foreach (TransportOperation transportOperation in context.Operations)
                {
                    transportOperation.Message.Headers.Remove(Headers.DiagnosticsTraceParent);
                    transportOperation.Message.Headers.Remove(Headers.DiagnosticsTraceState);
                    transportOperation.Message.Headers.Remove(Headers.DiagnosticsBaggage);
                }

                return next();
            }
        }

        class IncomingMessageHandler : IHandleMessages<IncomingMessage>
        {
            readonly Context testContext;

            public IncomingMessageHandler(Context testContext) => this.testContext = testContext;

            public Task Handle(IncomingMessage message, IMessageHandlerContext context)
            {
                testContext.MessageReceived = true;
                return Task.CompletedTask;
            }
        }
    }



    public class IncomingMessage : IMessage
    {
    }
}