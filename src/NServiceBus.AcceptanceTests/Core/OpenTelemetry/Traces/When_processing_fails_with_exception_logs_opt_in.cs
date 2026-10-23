namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using Configuration.AdvancedExtensibility;
using EndpointTemplates;
using NServiceBus;
using NUnit.Framework;

public class When_processing_fails_with_exception_logs_opt_in : OpenTelemetryAcceptanceTest
{
    [Test]
    public async Task Should_select_the_logs_mode()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<FailingEndpoint>(e => e
                .DoNotFailOnErrorMessages()
                .When(s => s.SendLocal(new FailingMessage())))
            .Run();

        Assert.That(context.FailedMessages, Has.Count.EqualTo(1), "the message should have failed");

        var activities = NServiceBusActivityListener.CompletedActivities;
        var exceptionEvents = activities.GetInvokedHandlerActivities().Concat(activities.GetReceiveMessageActivities()).SelectMany(a => a.Events).Where(e => e.Name == "exception");

        Assert.That(exceptionEvents, Is.Empty, "the opt-in selects the logs mode, which records no exception events");
    }

    public class Context : ScenarioContext;

    public class FailingEndpoint : EndpointConfigurationBuilder
    {
        // Does not call endpointConfiguration.Tracing(): the instrumentation options only come into existence while the endpoint is being created.
        public FailingEndpoint() => EndpointSetup<DefaultServer>(c => c.GetSettings().Set($"ACCEPTANCETEST_ENV:{OptInEnvironmentVariable}", "logs"));

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

    const string OptInEnvironmentVariable = "OTEL_SEMCONV_EXCEPTION_SIGNAL_OPT_IN";
    const string ErrorMessage = "boom!";
}