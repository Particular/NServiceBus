namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;

public class When_processing_fails : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_mark_span_as_failed()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<FailingEndpoint>(e => e
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .Done(c => c.HandlerInvoked).Run();

        Assert.That(context.FailedMessages.Count, Is.EqualTo(1), "the message should have failed");

        Activity failedPipelineActivity = NServicebusActivityListener.CompletedActivities.GetReceiveMessageActivities().Single();
        Assert.That(failedPipelineActivity.Status, Is.EqualTo(ActivityStatusCode.Error));
        Assert.That(failedPipelineActivity.StatusDescription, Is.EqualTo(ErrorMessage));

        var pipelineActivityTags = failedPipelineActivity.Tags.ToImmutableDictionary();
        pipelineActivityTags.VerifyTag("otel.status_code", "ERROR");
        pipelineActivityTags.VerifyTag("otel.status_description", ErrorMessage);

        Activity failedHandlerActivity = NServicebusActivityListener.CompletedActivities.GetInvokedHandlerActivities().Single();
        Assert.That(failedHandlerActivity.Status, Is.EqualTo(ActivityStatusCode.Error));
        Assert.That(failedHandlerActivity.StatusDescription, Is.EqualTo(ErrorMessage));

        var handlerActivityTags = failedHandlerActivity.Tags.ToImmutableDictionary();
        handlerActivityTags.VerifyTag("otel.status_code", "ERROR");
        handlerActivityTags.VerifyTag("otel.status_description", ErrorMessage);

    }

    class Context : ScenarioContext
    {
        public bool HandlerInvoked { get; set; }
    }

    class FailingEndpoint : EndpointConfigurationBuilder
    {
        public FailingEndpoint() => EndpointSetup<OpenTelemetryEnabledEndpoint>();

        class FailingMessageHandler : IHandleMessages<FailingMessage>
        {

            Context textContext;

            public FailingMessageHandler(Context textContext) => this.textContext = textContext;

            public Task Handle(FailingMessage message, IMessageHandlerContext context)
            {
                textContext.HandlerInvoked = true;
                throw new SimulatedException(ErrorMessage);
            }
        }
    }

    public class FailingMessage : IMessage
    {
    }

    const string ErrorMessage = "oh no!";
}