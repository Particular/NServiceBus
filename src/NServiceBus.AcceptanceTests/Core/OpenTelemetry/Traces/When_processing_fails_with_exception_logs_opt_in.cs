namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Traces;

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AcceptanceTesting;
using Configuration.AdvancedExtensibility;
using EndpointTemplates;
using NServiceBus;
using NUnit.Framework;

// Remove in v11 together with NServiceBus.Core/obsoletes-v11.cs, which defines the opt-in.
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

    // Read from Core, which keeps it internal, so the test stops working when v11 removes the opt-in.
    static readonly string OptInEnvironmentVariable = (string)(typeof(InstrumentationOptions)
        .GetField("ExceptionSignalOptInEnvironmentVariableKey", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("InstrumentationOptions.ExceptionSignalOptInEnvironmentVariableKey not found")).GetValue(null);

    const string ErrorMessage = "boom!";
}