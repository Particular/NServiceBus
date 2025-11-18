#nullable enable

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
        sendOptions.Context.Set(OpenTelemetrySendBehavior.StartNewTraceOnReceive, true);
    }

    /// <summary>
    /// Start a new OpenTelemetry trace conversation.
    /// </summary>
    /// <param name="publishOptions">The option being extended.</param>
    public static void ContinueExistingTraceOnReceive(this PublishOptions publishOptions)
    {
        publishOptions.Context.Set(OpenTelemetryPublishBehavior.ContinueTraceOnReceive, true);
    }
}