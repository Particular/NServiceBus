#nullable enable

namespace NServiceBus;

using Settings;

// This scaffolds a temporary, environment-variable-driven override for
// ExceptionRecordingMode so users can opt in ahead of time to the "logs"
// model described by the OTEL_SEMCONV_EXCEPTION_SIGNAL_OPT_IN convention:
// https://opentelemetry.io/docs/specs/semconv/exceptions/exceptions-logs/
// The environment variable is the highest-priority signal for this setting:
// when it's set to a recognized value it always wins, even over an
// explicitly configured ExceptionRecordingMode, so operators can force the
// exception-signal behavior without a code change/redeploy.
//
// In v12, once the exceptions-as-logs migration has settled, delete this
// entire file (SetExceptionRecordingModeDefault and
// ExceptionSignalOptInEnvironmentVariableKey), and remove the
// `Defaults(InstrumentationOptions.SetExceptionRecordingModeDefault);` call in
// OpenTelemetryFeature's constructor.
public partial class InstrumentationOptions
{
    internal static void SetExceptionRecordingModeDefault(SettingsHolder settings)
    {
        var options = settings.GetOrCreate<InstrumentationOptions>();

        var environment = settings.Get<SystemEnvironment>();
        var variableValue = environment.GetEnvironmentVariable(ExceptionSignalOptInEnvironmentVariableKey);

        options.ExceptionRecordingMode = variableValue switch
        {
            "logs" => ExceptionRecordingMode.Logs,
            "logs/dup" => ExceptionRecordingMode.SpanAndLogs,
            _ => options.ExceptionRecordingMode
        };
    }

    internal static readonly string ExceptionSignalOptInEnvironmentVariableKey = "OTEL_SEMCONV_EXCEPTION_SIGNAL_OPT_IN";
}
