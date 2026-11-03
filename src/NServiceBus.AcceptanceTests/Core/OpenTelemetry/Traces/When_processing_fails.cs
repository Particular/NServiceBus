namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using EndpointTemplates;
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
            .Run();

        Assert.That(context.FailedMessages, Has.Count.EqualTo(1), "the message should have failed");

        var failedPipelineActivity = NServiceBusActivityListener.CompletedActivities.GetReceiveMessageActivities().Single();
        var pipelineActivityTags = failedPipelineActivity.Tags.ToImmutableDictionary();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(failedPipelineActivity.Status, Is.EqualTo(ActivityStatusCode.Error));
            Assert.That(failedPipelineActivity.StatusDescription, Is.EqualTo(ErrorMessage));

            pipelineActivityTags.VerifyTag("otel.status_code", "ERROR");
            pipelineActivityTags.VerifyTag("otel.status_description", ErrorMessage);

            Assert.That(failedPipelineActivity.Events, Is.Empty,
                "the outer span should not duplicate the exception details already recorded on the inner span");
        }

        var failedHandlerActivity = NServiceBusActivityListener.CompletedActivities.GetInvokedHandlerActivities().Single();
        var handlerActivityTags = failedHandlerActivity.Tags.ToImmutableDictionary();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(failedHandlerActivity.Status, Is.EqualTo(ActivityStatusCode.Error));
            Assert.That(failedHandlerActivity.StatusDescription, Is.EqualTo(ErrorMessage));

            handlerActivityTags.VerifyTag("otel.status_code", "ERROR");
            handlerActivityTags.VerifyTag("otel.status_description", ErrorMessage);

            Assert.That(failedHandlerActivity.Events, Has.Exactly(1).Items,
                "the innermost span (the handler invocation) should record the exception details");
        }
    }

    public class Context : ScenarioContext;

    public class FailingEndpoint : EndpointConfigurationBuilder
    {
        public FailingEndpoint() => EndpointSetup<DefaultServer>();

        [Handler]
        public class FailingMessageHandler(Context textContext) : IHandleMessages<FailingMessage>
        {
            public Task Handle(FailingMessage message, IMessageHandlerContext context)
            {
                textContext.MarkAsCompleted();
                throw new SimulatedException(ErrorMessage);
            }
        }
    }

    public class FailingMessage : IMessage;

    const string ErrorMessage = "oh no!";
}