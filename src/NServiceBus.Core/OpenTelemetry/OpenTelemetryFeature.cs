#nullable enable

namespace NServiceBus;

using Features;

sealed class OpenTelemetryFeature : Feature
{
    public OpenTelemetryFeature() => Defaults(InstrumentationOptions.SetExceptionRecordingModeDefault);

    protected override void Setup(FeatureConfigurationContext context)
    {
        var instrumentationOptions = context.Settings.GetOrDefault<InstrumentationOptions>() ?? new InstrumentationOptions();

        context.Pipeline.Register(
            new OpenTelemetryPublishBehavior(instrumentationOptions),
            "Manages the depth of the trace for publishes"
        );

        context.Pipeline.Register(
            new OpenTelemetrySendBehavior(instrumentationOptions),
            "Manages the depth of the trace for sends"
        );

        context.Pipeline.Register(
            new PopulateRecoverabilityTraceMetadataBehavior(instrumentationOptions),
            "Populates the recoverability metadata"
        );

        context.Settings.AddStartupDiagnosticsSection("OpenTelemetry", new OpenTelemetryDiagnostics
        {
            SendTraceMode = instrumentationOptions.SendTraceMode.ToString(),
            PublishTraceMode = instrumentationOptions.PublishTraceMode.ToString(),
            ExceptionRecordingMode = instrumentationOptions.ExceptionRecordingMode.ToString(),
            Recoverability = new RecoverabilityInstrumentationDiagnostics
            {
                DelayedRetryTraceMode = instrumentationOptions.Recoverability.DelayedRetryTraceMode.ToString()
            },
            DelayedDelivery = new DelayedDeliveryInstrumentationDiagnostics
            {
                SendOperationTraceMode = instrumentationOptions.DelayedDelivery.SendOperationTraceMode.ToString(),
                SagaTimeoutTraceMode = instrumentationOptions.DelayedDelivery.SagaTimeoutTraceMode.ToString()
            },
            UseV11Behavior = V11BehaviorSwitch.UseV11Behavior
        }, StartupDiagnosticsJsonContext.Default.OpenTelemetryDiagnostics);
    }
}