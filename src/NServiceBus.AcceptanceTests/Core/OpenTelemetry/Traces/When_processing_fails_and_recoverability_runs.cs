namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_processing_fails_and_recoverability_runs : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_create_recoverability_span_as_child_of_failed_process_span()
    {
        await Scenario.Define<Context>()
            .WithEndpoint<RetryingEndpoint>(e => e
                .CustomConfig(c => c.Recoverability().Immediate(i => i.NumberOfRetries(1)))
                .When(s => s.SendLocal(new FailingMessage())))
            .Run();

        var failedProcess = NServiceBusActivityListener.CompletedActivities.GetReceiveMessageActivities().Single(a => a.Status == ActivityStatusCode.Error);
        var recoverability = NServiceBusActivityListener.CompletedActivities.GetRecoverabilityActivities().Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(recoverability.ParentId, Is.EqualTo(failedProcess.Id));
            Assert.That(recoverability.TraceId, Is.EqualTo(failedProcess.TraceId));
        }
    }

    [Test]
    public async Task Should_keep_recoverability_span_in_trace_of_process_span_when_new_trace_started()
    {
        await Scenario.Define<Context>()
            .WithEndpoint<RetryingEndpoint>(e => e
                .CustomConfig(c => c.Recoverability().Immediate(i => i.NumberOfRetries(1)))
                .When(s =>
                {
                    var sendOptions = new SendOptions();
                    sendOptions.RouteToThisEndpoint();
                    sendOptions.StartNewTraceOnReceive();
                    return s.Send(new FailingMessage(), sendOptions);
                }))
            .Run();

        var failedProcess = NServiceBusActivityListener.CompletedActivities.GetReceiveMessageActivities().Single(a => a.Status == ActivityStatusCode.Error);
        var recoverability = NServiceBusActivityListener.CompletedActivities.GetRecoverabilityActivities().Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(recoverability.ParentId, Is.EqualTo(failedProcess.Id));
            Assert.That(recoverability.TraceId, Is.EqualTo(failedProcess.TraceId), "recoverability span should not start a trace of its own");
        }
    }

    public class Context : ScenarioContext
    {
        public int InvocationCounter { get; set; }
    }

    public class RetryingEndpoint : EndpointConfigurationBuilder
    {
        public RetryingEndpoint() => EndpointSetup<DefaultServer>();

        [Handler]
        public class Handler(Context testContext) : IHandleMessages<FailingMessage>
        {
            public Task Handle(FailingMessage message, IMessageHandlerContext context)
            {
                testContext.InvocationCounter++;

                if (testContext.InvocationCounter == 1)
                {
                    throw new SimulatedException("first attempt fails");
                }

                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class FailingMessage : IMessage;
}
