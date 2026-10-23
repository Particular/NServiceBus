#nullable enable

namespace NServiceBus;

using Settings;

public partial class InstrumentationOptions
{
    /// <summary>
    /// Controls how exception details are recorded when an operation fails.
    /// Defaults to <see cref="NServiceBus.ExceptionRecordingMode.SpanAndLogs"/>: exceptions are logged when recoverability decides and recorded as an event on the activity.
    /// </summary>
    public ExceptionRecordingMode ExceptionRecordingMode { get; set; } = ExceptionRecordingMode.SpanAndLogs;

    internal static void SetExceptionRecordingModeDefault(SettingsHolder settings)
    {
        var options = settings.GetOrCreate<InstrumentationOptions>();

        var environment = settings.Get<SystemEnvironment>();
        var variableValue = environment.GetEnvironmentVariable(ExceptionSignalOptInEnvironmentVariableKey);

        // Recognized values per https://opentelemetry.io/docs/specs/semconv/exceptions/exceptions-logs/#recording-an-exception
        options.ExceptionRecordingMode = variableValue switch
        {
            "logs" => ExceptionRecordingMode.Logs,
            "logs/dup" => ExceptionRecordingMode.SpanAndLogs,
            _ => options.ExceptionRecordingMode
        };
    }

    internal static readonly string ExceptionSignalOptInEnvironmentVariableKey = "OTEL_SEMCONV_EXCEPTION_SIGNAL_OPT_IN";
}
