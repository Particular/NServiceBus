namespace NServiceBus.AcceptanceTests;

using System;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

// =============================================================================
// EVERYTHING IN THIS FILE IS TEMPORARY AND WILL BE REMOVED IN v11, together with
// the V11BehaviorSwitch block in NServiceBus.Core/obsoletes-v10.cs. The file is
// excluded from the shipped acceptance test sources in the project file.
// =============================================================================

// Runs the test with the NServiceBus.Core.OpenTelemetry.UseV11Behavior AppContext switch enabled, i.e. with
// the OpenTelemetry defaults of v11. The switch value is cached process-wide, so the cache is reset through
// the internal V11BehaviorSwitch.ResetUseV11Behavior method. Only safe in [NonParallelizable] fixtures such
// as OpenTelemetryAcceptanceTest. In v11 these defaults are the only behavior: delete this attribute and
// remove it from the tests that use it.
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true)]
public sealed class OpenTelemetryV11DefaultsAttribute : Attribute, ITestAction
{
    const string SwitchName = "NServiceBus.Core.OpenTelemetry.UseV11Behavior";

    static readonly MethodInfo ResetSwitch = typeof(EndpointConfiguration).Assembly
        .GetType("NServiceBus.V11BehaviorSwitch", throwOnError: true)!
        .GetMethod("ResetUseV11Behavior", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("NServiceBus.V11BehaviorSwitch.ResetUseV11Behavior not found");

    public ActionTargets Targets => ActionTargets.Test;

    public void BeforeTest(ITest test) => Set(true);

    public void AfterTest(ITest test) => Set(false);

    static void Set(bool enabled)
    {
        AppContext.SetSwitch(SwitchName, enabled);
        ResetSwitch.Invoke(null, null);
    }
}

public static class ExceptionSignalOptIn
{
    public const string EnvironmentVariable = "OTEL_SEMCONV_EXCEPTION_SIGNAL_OPT_IN";
    public const string Logs = "logs";
    public const string SpanAndLogs = "logs/dup";

    public static void SetExceptionSignalOptIn(this EndpointConfiguration endpointConfiguration, string value) =>
        NServiceBus.Configuration.AdvancedExtensibility.AdvancedExtensibilityExtensions.GetSettings(endpointConfiguration)
            .Set($"ACCEPTANCETEST_ENV:{EnvironmentVariable}", value);
}

// Covers usage reporting with the v11 metrics, where the reporter counts processing attempts without error.type.
// ServicePlatform/UsageReporting/When_send_usage_info_enabled covers the pre-v11 successes counter.
[NonParallelizable]
public class When_send_usage_info_enabled_with_v11_defaults : NServiceBusAcceptanceTest
{
    static readonly TimeSpan ReportingInterval = TimeSpan.FromSeconds(1);

    [Test]
    [OpenTelemetryV11Defaults]
    public async Task Reports_successfully_processed_messages_only()
    {
        var context = await NServiceBus.AcceptanceTesting.Scenario.Define<Context>(ctx => ctx.FailOnNextMessage = true)
            .WithEndpoint<ServiceControl>()
            .WithEndpoint<EndpointThatReportsUsageInfo>(b => b
                .When(async session =>
                {
                    await session.SendLocal(new BusinessMessage());
                    await session.SendLocal(new BusinessMessage());
                    await session.SendLocal(new BusinessMessage());
                }))
            .Done(ctx => ctx.UsageReportedToServiceControl >= 3)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.FailedAttempts, Is.EqualTo(1));
            Assert.That(context.UsageReportedToServiceControl, Is.EqualTo(3));
        }
    }

    public class Context : NServiceBus.AcceptanceTesting.ScenarioContext
    {
        public bool FailOnNextMessage { get; set; }
        public int FailedAttempts { get; set; }
        public long UsageReportedToServiceControl { get; set; }
    }

    public class BusinessMessage : IMessage;

    public class EndpointUsageReport
    {
        public required string EndpointName { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
        public long MessagesSuccessfullyProcessed { get; set; }
    }

    public class EndpointThatReportsUsageInfo : NServiceBus.AcceptanceTesting.EndpointConfigurationBuilder
    {
        public EndpointThatReportsUsageInfo() => EndpointSetup<EndpointTemplates.DefaultServer>(endpointConfiguration =>
        {
            endpointConfiguration.LimitMessageProcessingConcurrencyTo(1);
            _ = endpointConfiguration.Recoverability().Immediate(x => x.NumberOfRetries(1));

            endpointConfiguration.ReportUsageInformationToServiceControl();
            NServiceBus.Configuration.AdvancedExtensibility.AdvancedExtensibilityExtensions.GetSettings(endpointConfiguration)
                .Set("UsageReporting.ReportingInterval", ReportingInterval);

            endpointConfiguration.ServiceControlErrorInstanceQueue(NServiceBus.AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(ServiceControl)));
        });

        [Handler]
        public class BusinessMessageHandler(Context scenarioContext) : IHandleMessages<BusinessMessage>
        {
            public Task Handle(BusinessMessage message, IMessageHandlerContext context)
            {
                if (scenarioContext.FailOnNextMessage)
                {
                    scenarioContext.FailOnNextMessage = false;
                    scenarioContext.FailedAttempts += 1;
                    throw new Exception("Boom");
                }

                return Task.CompletedTask;
            }
        }
    }

    public class ServiceControl : NServiceBus.AcceptanceTesting.EndpointConfigurationBuilder
    {
        public ServiceControl() => EndpointSetup<EndpointTemplates.DefaultServer>(endpointConfiguration =>
        {
            endpointConfiguration.LimitMessageProcessingConcurrencyTo(1);
            _ = endpointConfiguration.UseSerialization<SystemJsonSerializer>();
            endpointConfiguration.Pipeline.Register(new FixEnclosedMessageTypeBehavior(), "Maps the usage report to the message type of this test");
        });

        class FixEnclosedMessageTypeBehavior : NServiceBus.Pipeline.Behavior<NServiceBus.Pipeline.IIncomingPhysicalMessageContext>
        {
            public override Task Invoke(NServiceBus.Pipeline.IIncomingPhysicalMessageContext context, Func<Task> next)
            {
                if (context.Message.Headers.TryGetValue(Headers.EnclosedMessageTypes, out var messageType) && messageType.Contains(nameof(EndpointUsageReport)))
                {
                    context.Message.Headers[Headers.EnclosedMessageTypes] = typeof(EndpointUsageReport).FullName;
                }

                return next();
            }
        }

        [Handler]
        public class EndpointUsageReportHandler(Context scenarioContext) : IHandleMessages<EndpointUsageReport>
        {
            public Task Handle(EndpointUsageReport message, IMessageHandlerContext context)
            {
                scenarioContext.UsageReportedToServiceControl += message.MessagesSuccessfullyProcessed;
                return Task.CompletedTask;
            }
        }
    }
}
