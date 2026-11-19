namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NServiceBus;
using NUnit.Framework;

public class When_processing_fails_with_exception_logs_opt_in : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_select_the_logs_mode()
    {
        var exceptionEvents = await RunWithOptIn(ExceptionSignalOptIn.Logs);

        Assert.That(exceptionEvents, Is.Empty, "the opt-in selects the logs mode, which records no exception events");
    }

    [Test]
    [OpenTelemetryV11Defaults]
    public async Task Should_ignore_the_opt_in_with_v11_behavior()
    {
        var exceptionEvents = await RunWithOptIn(ExceptionSignalOptIn.SpanAndLogs);

        Assert.That(exceptionEvents, Is.Empty, "with the v11 behavior exceptions are only logged, whatever the opt-in says");
    }

    async Task<string[]> RunWithOptIn(string optIn)
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<FailingEndpoint>(e => e
                .CustomConfig(c => c.SetExceptionSignalOptIn(optIn))
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .Run();

        Assert.That(context.FailedMessages, Has.Count.EqualTo(1), "the message should have failed");

        var activities = NServiceBusActivityListener.CompletedActivities;
        return [.. activities.GetInvokedHandlerActivities().Concat(activities.GetReceiveMessageActivities()).SelectMany(a => a.Events).Where(e => e.Name == "exception").Select(e => e.Name)];
    }

    public class Context : ScenarioContext;

    public class FailingEndpoint : EndpointConfigurationBuilder
    {
        public FailingEndpoint() => EndpointSetup<DefaultServer>();

        [Handler]
        public class FailingMessageHandler(Context testContext) : IHandleMessages<FailingMessage>
        {
            public Task Handle(FailingMessage message, IMessageHandlerContext context)
            {
                testContext.MarkAsCompleted();
                throw new SimulatedException(ErrorMessage);
            }
        }
    }

    public class FailingMessage : IMessage;

    const string ErrorMessage = "boom!";
}
