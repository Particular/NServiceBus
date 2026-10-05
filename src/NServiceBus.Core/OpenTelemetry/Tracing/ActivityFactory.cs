#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pipeline;
using Transport;

sealed partial class ActivityFactory(InstrumentationOptions options) : IActivityFactory
{
    public InstrumentationOptions Options { get; } = options;

    static Activity? StartActivityFromIncomingMessage(ActivitySource activitySource, string activityName, Dictionary<string, string> headers, string nativeMessageId)
    {
        // CreateActivity is a no-op if there are no listeners but we are doing a fast path check
        // here nonetheless to avoid having to parse headers, access the extension bag, etc.
        if (!activitySource.HasListeners())
        {
            return null;
        }

        var senderContextExists = TryParseSenderContext(headers, out ActivityContext senderContext);
        var startNewTrace = false;

        Activity? activity;

        if (senderContextExists) // create a child from a logical send
        {
            startNewTrace = headers.TryGetValue(Headers.StartNewTrace, out var startNewTraceHeaderValue)
                            && string.Equals(startNewTraceHeaderValue, bool.TrueString, StringComparison.OrdinalIgnoreCase);

            if (startNewTrace)
            {
                // Create a brand-new trace and link the span to the NSB sender span.
                // An activity without a parent context adopts Activity.Current as its parent when it
                // starts, so Current has to be cleared. See: https://github.com/dotnet/runtime/issues/65528#issuecomment-2613486896
                Activity.Current = null;
                activity = activitySource.CreateActivity(activityName, ActivityKind.Consumer, parentContext: default, links: [new ActivityLink(senderContext)]);
            }
            else if (V11BehaviorSwitch.UseV11Behavior && Activity.Current != null) // remove the switch check in v11, see obsoletes-v10.cs
            {
                // A transport SDK receive span is ambient: make it the parent (an activity without
                // a parent context adopts Activity.Current when it starts) and link to the NSB sender span.
                activity = activitySource.CreateActivity(activityName, ActivityKind.Consumer, parentContext: default, links: [new ActivityLink(senderContext)]);
            }
            else
            {
                // Create a span that is a child of the NSB sender span
                activity = activitySource.CreateActivity(activityName, ActivityKind.Consumer, parentContext: senderContext);
            }
        }
        else
        {
            // No NServiceBus trace context on the message, so there is nothing for NServiceBus to
            // propagate from the headers: trace state and baggage are only meaningful together with
            // a trace parent. The span adopts Activity.Current as parent, if available, and inherits
            // whatever trace state and baggage that activity carries through the parent chain.
            activity = activitySource.CreateActivity(activityName, ActivityKind.Consumer, parentContext: default);
        }

        if (activity is null)
        {
            return null;
        }

        // The id format can only be set on an activity that has not started yet. It is forced to W3C
        // so an ambient hierarchical activity does not leak its format into the trace headers.
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.AddTag(ActivityTags.NativeMessageId, nativeMessageId);

        // IsAllDataRequested is false when a listener sampled this as PropagationData-only: it wants the
        // activity to exist for context propagation but won't read anything beyond that, so skip the tag work.
        // The trace state and baggage propagation after Start() is correctness, not enrichment, and runs regardless.
        if (activity.IsAllDataRequested)
        {
            ActivityDecorator.PromoteHeadersToTags(activity, headers);
        }

        // Start before reading the headers: Activity.Parent is only assigned by Start(), and the
        // baggage propagation below skips keys the parent chain already carries.
        activity.Start();

        if (senderContextExists)
        {
            // The message carries NServiceBus trace context, so the trace state and baggage headers
            // that travel with it are NServiceBus' responsibility.
            if (!startNewTrace)
            {
                // Trace state belongs to the trace it was recorded in, so it is not carried into a new trace.
                ContextPropagation.PropagateTraceStateFromHeaders(activity, headers);
            }

            // Baggage is always propagated, also when a transport SDK span is the parent: none of the
            // supported SDKs propagate baggage yet.
            ContextPropagation.PropagateBaggageFromHeaders(activity, headers);
        }

        return activity;
    }

    static bool TryParseSenderContext(Dictionary<string, string> headers, out ActivityContext senderContext)
    {
        senderContext = default;

        // The NServiceBus-specific header takes precedence because a transport SDK may have
        // overwritten the W3C header with its own context.
        if (headers.TryGetValue(Headers.NServiceBusDiagnosticsTraceParent, out var senderSpanId))
        {
            if (ActivityContext.TryParse(senderSpanId, null, isRemote: true, out senderContext))
            {
                return true;
            }
        }

        // The W3C header is the fallback, so messages from endpoints on older versions,
        // which only write that one, still continue the trace.
        if (headers.TryGetValue(Headers.DiagnosticsTraceParent, out senderSpanId))
        {
            if (ActivityContext.TryParse(senderSpanId, null, isRemote: true, out senderContext))
            {
                return true;
            }
        }

        return false;
    }

    public Activity? StartIncomingPipelineActivity(MessageContext context)
    {
        var activity = StartActivityFromIncomingMessage(
            ActivitySources.Main,
            ActivityNames.IncomingMessageActivityName,
            context.Headers,
            context.NativeMessageId);

        if (activity is null)
        {
            return activity;
        }

        activity.DisplayName = V11BehaviorSwitch.UseV11Behavior
            ? $"{ActivityDisplayNames.ProcessOperation} {context.ReceiveAddress}"
            : ActivityDisplayNames.ProcessMessage;

        return activity;
    }

    public Activity? StartOutgoingPipelineActivity(string activityName, string displayName, IBehaviorContext outgoingContext)
    {
        var activity = ActivitySources.Main.CreateActivity(activityName, ActivityKind.Producer);
        if (activity == null)
        {
            return activity;
        }

        return StartOutgoingPipelineActivity(activity, displayName, outgoingContext);
    }

    public Activity? StartOutgoingPipelineActivity(string activityName, string legacyDisplayName, string operation, Type messageType, IBehaviorContext outgoingContext)
    {
        var activity = ActivitySources.Main.CreateActivity(activityName, ActivityKind.Producer);
        if (activity == null)
        {
            return activity;
        }

        // Span names follow the OTel messaging convention "{operation} {message type}" with the v11 behavior.
        // Remove the switch check and legacyDisplayName in v11, see obsoletes-v10.cs.
        var displayName = V11BehaviorSwitch.UseV11Behavior
            ? $"{operation} {messageType.Name}"
            : legacyDisplayName;

        return StartOutgoingPipelineActivity(activity, displayName, outgoingContext);
    }

    public Activity? StartOutgoingPipelineActivity(string activityName, string legacyDisplayName, string operation, Type[] messageTypes, IBehaviorContext outgoingContext)
    {
        var activity = ActivitySources.Main.CreateActivity(activityName, ActivityKind.Producer);
        if (activity == null)
        {
            return activity;
        }

        // Remove the switch check and legacyDisplayName in v11, see obsoletes-v10.cs.
        var displayName = V11BehaviorSwitch.UseV11Behavior
            ? $"{operation} {string.Join(' ', messageTypes.Select(x => x.Name))}"
            : legacyDisplayName;

        return StartOutgoingPipelineActivity(activity, displayName, outgoingContext);
    }

    static Activity StartOutgoingPipelineActivity(Activity activity, string displayName, IBehaviorContext outgoingContext)
    {
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.DisplayName = displayName;
        activity.Start();

        outgoingContext.Extensions.SetOutgoingPipelineActivity(activity);

        return activity;
    }

    public Activity? StartHandlerActivity(MessageHandler messageHandler)
    {
        if (Activity.Current == null)
        {
            // don't call StartActivity if we haven't started an activity from the incoming pipeline to avoid the handlers being sampled although the incoming message isn't.
            return null;
        }

        // Until v11 the dedicated handler source is opt-in; existing configurations only
        // subscribe to the main source and must keep receiving handler spans from it.
        // Remove the switch check in v11, see obsoletes-v10.cs.
        var source = V11BehaviorSwitch.UseV11Behavior
            ? ActivitySources.Handler
            : ActivitySources.Main;

        var activity = source.StartActivity(ActivityNames.InvokeHandlerActivityName);

        if (activity is null)
        {
            return activity;
        }

        activity.DisplayName = messageHandler.HandlerType.Name;
        activity.AddTag(ActivityTags.HandlerType, messageHandler.HandlerType.FullName);
        return activity;
    }

    public Activity? StartRecoverabilityActivity(ErrorContext context)
    {
        var activity = StartActivityFromIncomingMessage(
            ActivitySources.Recoverability,
            ActivityNames.RecoverabilityActivityName,
            context.Headers,
            context.NativeMessageId);

        if (activity is null)
        {
            return activity;
        }

        activity.DisplayName = ActivityDisplayNames.Recoverability;

        return activity;
    }

    public void UpdateActivityFromRecoverabilityAction(Activity activity, RecoverabilityAction recoverabilityAction, string receiveAddress)
    {
        // Nothing below is read unless a listener asked for full data (IsAllDataRequested), so bail out early
        // rather than building tags and DisplayName strings for an activity nobody will inspect.
        if (!activity.IsAllDataRequested)
        {
            return;
        }

        if (recoverabilityAction is ImmediateRetry)
        {
            activity.AddTag(ActivityTags.RecoverabilityAction, "immediate_retry");
            activity.DisplayName = ActivityDisplayNames.ImmediateRetryOperation;

            if (V11BehaviorSwitch.UseV11Behavior)
            {
                activity.DisplayName += $" {receiveAddress}";
            }
        }
        else if (recoverabilityAction is DelayedRetry)
        {
            activity.AddTag(ActivityTags.RecoverabilityAction, "delayed_retry");
            activity.DisplayName = ActivityDisplayNames.DelayedRetryOperation;

            if (V11BehaviorSwitch.UseV11Behavior)
            {
                activity.DisplayName += $" {receiveAddress}";
            }
        }
        else if (recoverabilityAction is MoveToError moveToError)
        {
            activity.AddTag(ActivityTags.RecoverabilityAction, "move_to_error");

            activity.DisplayName = V11BehaviorSwitch.UseV11Behavior
                ? $"{ActivityDisplayNames.MoveToErrorOperation} {moveToError.ErrorQueue}"
                : $"{ActivityDisplayNames.MoveToErrorOperation} error";
        }
        else if (recoverabilityAction is Discard)
        {
            activity.AddTag(ActivityTags.RecoverabilityAction, "discard");
            activity.DisplayName = ActivityDisplayNames.DiscardOperation;
        }
    }

    public void RecordError(Activity? activity, Exception exception, IServiceProvider serviceProvider)
    {
        if (activity == null)
        {
            return;
        }

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.SetTag(ActivityTags.ErrorType, exception.GetType().FullName);

        // Legacy tags apply per-activity (each activity on the way up the pipeline gets its own),
        // unlike the exception event below which is deduped once per exception via ExceptionRecordedFlag.
        // Only the "is this worth computing" part is guarded by IsAllDataRequested.
        if (activity.IsAllDataRequested && !V11BehaviorSwitch.UseV11Behavior) // Removed in v11, see obsoletes-v10.cs
        {
            LegacyExceptionTags.SetLegacyStatusTags(activity, exception);
        }

        if (!exception.Data.Contains(ExceptionRecordedFlag))
        {
            if (Options.ExceptionRecordingMode == ExceptionRecordingMode.Logs)
            {
                // The factory is created before the container exists, so the logger is resolved on first use.
                logger ??= serviceProvider.GetRequiredService<ILogger<ActivityFactory>>();
                LogExceptionWhileExecuting(logger, exception, activity.DisplayName);
            }
            else if (activity.IsAllDataRequested)
            {
                // Building the exception event (stack trace) is wasted work when nothing downstream will read it.
                activity.AddException(exception, V11BehaviorSwitch.UseV11Behavior ? default : LegacyExceptionTags.EscapedTagList); // drop the tag list in v11, see obsoletes-v10.cs
            }

            exception.Data[ExceptionRecordedFlag] = true;
        }

        if (exception is TaskCanceledException)
        {
            activity.SetTag(ActivityTags.CancelledTask, true);
        }
    }

    const string ExceptionRecordedFlag = "otel.exception.recorded";

    ILogger? logger;

    [LoggerMessage(LogLevel.Error, "An exception occurred while executing '{DisplayName}'.")]
    static partial void LogExceptionWhileExecuting(ILogger logger, Exception exception, string displayName);
}