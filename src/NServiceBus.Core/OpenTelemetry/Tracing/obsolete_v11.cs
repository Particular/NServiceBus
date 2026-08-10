#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Particular.Obsoletes;

// =============================================================================
// EVERYTHING IN THIS FILE IS TEMPORARY AND WILL BE REMOVED IN v11.
//
// In v10.3, switching to System.Diagnostics.DistributedContextPropagator for
// OpenTelemetry trace-context/baggage propagation changes the baggage wire
// format (W3C OWS encoding + whitespace trimming) and is therefore breaking on
// rolling upgrades. To stay backwards compatible it is opt-in via an AppContext
// switch and the legacy propagator below remains the default.
//
// In v11 the new propagator becomes the default: delete this entire file and
// remove the two `if (!ObsoleteV11.UseDistributedContextPropagator)` delegation
// blocks in ContextPropagation.cs.
// =============================================================================
static class LegacyContextPropagation
{
    enum SwitchState : byte
    {
        Unchecked = 0,
        Enabled = 1,
        Disabled = 2
    }

    static SwitchState cachedUseDistributedContextPropagator;

    [PreObsolete("https://github.com/Particular/NServiceBus/issues/7825",
        Note = "In v11, DistributedContextPropagator-based context propagation becomes the default and this switch will be removed together with the legacy propagator in obsolete_v11.cs.",
        ReplacementTypeOrMember = "ContextPropagation")]
    public const string UseDistributedContextPropagatorSwitchName = "NServiceBus.Core.OpenTelemetry.UseDistributedContextPropagator";

    [PreObsolete("https://github.com/Particular/NServiceBus/issues/7825",
        Note = "In v11, DistributedContextPropagator-based context propagation becomes the default and this switch will be removed together with the legacy propagator in obsolete_v11.cs.",
        ReplacementTypeOrMember = "ContextPropagation")]
    public static bool UseDistributedContextPropagator
    {
        get
        {
            var state = cachedUseDistributedContextPropagator;
            if (state != SwitchState.Unchecked)
            {
                return state == SwitchState.Enabled;
            }

            state = AppContext.TryGetSwitch(UseDistributedContextPropagatorSwitchName, out var isEnabled) && isEnabled
                ? SwitchState.Enabled
                : SwitchState.Disabled;
            cachedUseDistributedContextPropagator = state;

            return state == SwitchState.Enabled;
        }
    }

    internal static void ResetUseDistributedContextPropagator() => cachedUseDistributedContextPropagator = SwitchState.Unchecked;

    public static void PropagateContextToHeaders(Activity? activity, Dictionary<string, string> headers)
    {
        if (activity is null)
        {
            return;
        }

        if (activity.Id is not null)
        {
            headers[Headers.DiagnosticsTraceParent] = activity.Id;
        }

        if (activity.TraceStateString is not null)
        {
            headers[Headers.DiagnosticsTraceState] = activity.TraceStateString;
        }

        var baggage = string.Join(",", activity.Baggage.Select(item => $"{item.Key}={Uri.EscapeDataString(item.Value ?? string.Empty)}"));
        if (!string.IsNullOrEmpty(baggage))
        {
            headers[Headers.DiagnosticsBaggage] = baggage;
        }
    }

    public static void PropagateContextFromHeaders(Activity? activity, IDictionary<string, string> headers)
    {
        if (activity is null)
        {
            return;
        }

        if (headers.TryGetValue(Headers.DiagnosticsTraceState, out var traceState))
        {
            activity.TraceStateString = traceState;
        }

        if (headers.TryGetValue(Headers.DiagnosticsBaggage, out var baggageValue))
        {
            var baggageSpan = baggageValue.AsSpan();
            // HINT: Iterate in reverse order because Activity baggage is LIFO
            while (!baggageSpan.IsEmpty)
            {
                var lastComma = baggageSpan.LastIndexOf(',');
                ReadOnlySpan<char> baggageItem;

                if (lastComma >= 0)
                {
                    baggageItem = baggageSpan[(lastComma + 1)..];
                    baggageSpan = baggageSpan[..lastComma];
                }
                else
                {
                    baggageItem = baggageSpan;
                    baggageSpan = [];
                }

                var firstEquals = baggageItem.IndexOf('=');
                if (firstEquals < 0 || firstEquals >= baggageItem.Length)
                {
                    continue;
                }

                var key = baggageItem[..firstEquals].Trim();
                var value = baggageItem[(firstEquals + 1)..];
                activity.AddBaggage(key.ToString(), Uri.UnescapeDataString(value));
            }
        }
    }
}

// Handler spans move from the "NServiceBus.Core" ActivitySource to the dedicated
// "NServiceBus.Core.Handler" source so they can be filtered/sampled independently
// (https://github.com/Particular/NServiceBus/issues/7284). Existing OpenTelemetry
// configurations only subscribe to "NServiceBus.Core" and would silently lose handler
// spans, so the new source is opt-in via an AppContext switch until v11.
//
// In v11 the dedicated source becomes the default: delete this class and remove the
// source selection in ActivityFactory.StartHandlerActivity so it always uses
// ActivitySources.Handler.
static class HandlerActivitySourceSwitch
{
    enum SwitchState : byte
    {
        Unchecked = 0,
        Enabled = 1,
        Disabled = 2
    }

    static SwitchState cachedUseHandlerActivitySource;

    [PreObsolete("https://github.com/Particular/NServiceBus/issues/7284",
        Note = "In v11, handler spans are always emitted from the NServiceBus.Core.Handler ActivitySource and this switch will be removed.",
        ReplacementTypeOrMember = "ActivitySources.Handler")]
    public const string UseHandlerActivitySourceSwitchName = "NServiceBus.Core.OpenTelemetry.UseHandlerActivitySource";

    [PreObsolete("https://github.com/Particular/NServiceBus/issues/7284",
        Note = "In v11, handler spans are always emitted from the NServiceBus.Core.Handler ActivitySource and this switch will be removed.",
        ReplacementTypeOrMember = "ActivitySources.Handler")]
    public static bool UseHandlerActivitySource
    {
        get
        {
            var state = cachedUseHandlerActivitySource;
            if (state != SwitchState.Unchecked)
            {
                return state == SwitchState.Enabled;
            }

            state = AppContext.TryGetSwitch(UseHandlerActivitySourceSwitchName, out var isEnabled) && isEnabled
                ? SwitchState.Enabled
                : SwitchState.Disabled;
            cachedUseHandlerActivitySource = state;

            return state == SwitchState.Enabled;
        }
    }

    internal static void ResetUseHandlerActivitySource() => cachedUseHandlerActivitySource = SwitchState.Unchecked;
}


// This class bridges two independent legacy exception-tagging behaviors, both
// scheduled for removal in v11:
//
// - SetLegacyStatusTags sets the "otel.status_code"/"otel.status_description"
//   tags, which predate native support for Activity.SetStatus/Activity.Status
//   and are now redundant with it. Kept only for consumers still reading the
//   tags directly instead of Activity.Status.
// - EscapedTagList carries "exception.escaped", an attribute the OTel semantic
//   conventions have marked Deprecated:
//   https://opentelemetry.io/docs/specs/semconv/exceptions/exceptions-logs/
//   It's added to the exception event for backward compatibility with
//   existing consumers of that attribute.
//
// In v11, delete this entire class, remove the
// `LegacyExceptionTags.SetLegacyStatusTags(activity, exception);` call in
// ActivityFactory.RecordError, and stop passing EscapedTagList to
// activity.AddException in the same method.
static class LegacyExceptionTags
{
    public static void SetLegacyStatusTags(Activity activity, Exception exception)
    {
        activity.SetTag("otel.status_code", "ERROR");
        activity.SetTag("otel.status_description", exception.Message);
    }

    public static TagList EscapedTagList { get; } = new() { { "exception.escaped", true } };
}