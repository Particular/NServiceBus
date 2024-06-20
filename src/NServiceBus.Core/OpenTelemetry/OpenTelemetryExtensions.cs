namespace NServiceBus;

/// <summary>
/// Gives users control over the depth of an OpenTelemetry trace.
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Start a new OpenTelemetry trace conversation.
    /// </summary>
    /// <param name="sendOptions">The option being extended.</param>
    public static void StartNewTraceOnReceive(this SendOptions sendOptions)
    {
        sendOptions.SetHeader(Headers.StartNewTrace, bool.TrueString);
    }

    /// <summary>
    /// Start a new OpenTelemetry trace conversation.
    /// </summary>
    /// <param name="publishOptions">The option being extended.</param>
    public static void StartNewTraceOnReceive(this PublishOptions publishOptions)
    {
        publishOptions.SetHeader(Headers.StartNewTrace, bool.TrueString);
    }
}