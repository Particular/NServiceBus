#nullable enable

namespace NServiceBus;

using Settings;

// This scaffolds a temporary, environment-variable-driven default for
// ExceptionRecordingMode so users can opt in ahead of time to the "logs"
// model described by the OTEL_SEMCONV_EXCEPTION_SIGNAL_OPT_IN convention:
// https://opentelemetry.io/docs/specs/semconv/exceptions/exceptions-logs/
// without NServiceBus committing to that behavior as the default yet.
//
// In v12, once the exceptions-as-logs migration has settled, delete this
// entire file (ExceptionRecordingModeSetByUser, SetExceptionRecordingModeDefault,
// and ExceptionSignalOptInEnvironmentVariableKey), remove the
// `Defaults(InstrumentationOptions.SetExceptionRecordingModeDefault);` call in
// OpenTelemetryFeature's constructor, and simplify the ExceptionRecordingMode
// property in InstrumentationOptions.cs back to a plain auto-property with a
// hardcoded default, since ExceptionRecordingModeSetByUser is its only consumer.
public partial class InstrumentationOptions
{
    bool ExceptionRecordingModeSetByUser;

    // Only applies a default when the user hasn't engaged with Tracing() at all - if an
    // InstrumentationOptions is already present, it's respected wholesale, even if the user
    // only touched a different property and left ExceptionRecordingMode at its hardcoded
    // default. This matches the granularity of the settings object as a whole rather than
    // tracking each property individually.
    internal static void SetExceptionRecordingModeDefault(SettingsHolder settings)
    {
        var options = settings.GetOrDefault<InstrumentationOptions>();
        if (options is not null && options.ExceptionRecordingModeSetByUser)
        {
            return;
        }

        var environment = settings.Get<SystemEnvironment>();
        var variableValue = environment.GetEnvironmentVariable(ExceptionSignalOptInEnvironmentVariableKey);

        var instrumentationOptions = new InstrumentationOptions();
        instrumentationOptions.ExceptionRecordingMode = variableValue switch
        {
            "logs" => ExceptionRecordingMode.Logs,
            "logs/dup" => ExceptionRecordingMode.SpanAndLogs,
            _ => instrumentationOptions.ExceptionRecordingMode
        };

        settings.Set(instrumentationOptions);
    }

    internal static readonly string ExceptionSignalOptInEnvironmentVariableKey = "OTEL_SEMCONV_EXCEPTION_SIGNAL_OPT_IN";
}