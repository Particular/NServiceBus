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
    /// Start a new OpenTelemetry trace on receive of this message, linked back to the send span.
    /// Overrides <see cref="InstrumentationOptions.SendTraceMode"/> for this message.
    /// </summary>
    /// <param name="sendOptions">The option being extended.</param>
    public static void StartNewTraceOnReceive(this SendOptions sendOptions)
    {
        ArgumentNullException.ThrowIfNull(sendOptions);
        sendOptions.Context.Set(TraceConnectorOverrideKey, TraceMode.StartNew);
    }

    /// <summary>
    /// Continue the existing OpenTelemetry trace on receive of this message.
    /// Overrides <see cref="InstrumentationOptions.SendTraceMode"/> for this message.
    /// </summary>
    /// <param name="sendOptions">The option being extended.</param>
    public static void ContinueExistingTraceOnReceive(this SendOptions sendOptions)
    {
        ArgumentNullException.ThrowIfNull(sendOptions);
        sendOptions.Context.Set(TraceConnectorOverrideKey, TraceMode.ContinueExisting);
    }

    /// <summary>
    /// Start a new OpenTelemetry trace on receive of this event, linked back to the publish span.
    /// Overrides <see cref="InstrumentationOptions.PublishTraceMode"/> for this message.
    /// </summary>
    /// <param name="publishOptions">The option being extended.</param>
    public static void StartNewTraceOnReceive(this PublishOptions publishOptions)
    {
        ArgumentNullException.ThrowIfNull(publishOptions);
        publishOptions.Context.Set(TraceConnectorOverrideKey, TraceMode.StartNew);
    }

    /// <summary>
    /// Continue the existing OpenTelemetry trace on receive of this event.
    /// Overrides <see cref="InstrumentationOptions.PublishTraceMode"/> for this message.
    /// </summary>
    /// <param name="publishOptions">The option being extended.</param>
    public static void ContinueExistingTraceOnReceive(this PublishOptions publishOptions)
    {
        ArgumentNullException.ThrowIfNull(publishOptions);
        publishOptions.Context.Set(TraceConnectorOverrideKey, TraceMode.ContinueExisting);
    }

    internal const string TraceConnectorOverrideKey = "NServiceBus.OpenTelemetry.TraceConnectorOverride";
}