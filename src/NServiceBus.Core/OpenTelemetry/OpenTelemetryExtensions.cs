#nullable enable

namespace NServiceBus;

using System;

/// <summary>
/// Gives users control over the depth of an OpenTelemetry trace.
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Provides access to instrumentation options for OpenTelemetry tracing.
    /// </summary>
    /// <param name="config">The endpoint configuration.</param>
    /// <returns>The <see cref="InstrumentationOptions"/> instance for this endpoint.</returns>
    public static InstrumentationOptions Tracing(this EndpointConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return config.Settings.GetOrCreate<InstrumentationOptions>();
    }

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