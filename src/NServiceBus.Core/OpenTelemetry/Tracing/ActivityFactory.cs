#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Extensibility;
using Logging;
using Pipeline;
using Transport;

sealed class ActivityFactory(InstrumentationOptions options) : IActivityFactory
{
    public InstrumentationOptions Options { get; } = options;

    static Activity? CreateActivityFromIncomingMessage(ActivitySource activitySource, string activityName, Dictionary<string, string> headers, string nativeMessageId, ContextBag extensions)
    {
        // CreateActivity is a no-op if there are no listeners but we are doing a fast path check
        // here nonetheless to avoid having to parse headers, access the extension bag, etc.
        if (!activitySource.HasListeners())
        {
            return null;
        }

        var senderContextExists = false;
        ActivityContext senderContext;

        if (headers.TryGetValue(Headers.DiagnosticsTraceParent, out var senderSpanId))
        {
            senderContextExists = ActivityContext.TryParse(senderSpanId, null, isRemote: true, out senderContext);
        }

        Activity? activity;

        if (extensions.TryGet<Activity>(out var transportActivity))
        {
            // Create a child span of the transport span and link to the NSB sender span
            activity = activitySource.CreateActivity(
                activityName,
                ActivityKind.Consumer,
                transportActivity.Context,
                links: senderContextExists ? [new ActivityLink(senderContext)] : null);
        }
        else if (senderContextExists) // otherwise directly create a child from a logical send
        {
            if (headers.TryGetValue(Headers.StartNewTrace, out var startNewTrace) &&
                string.Equals(startNewTrace, bool.TrueString, StringComparison.OrdinalIgnoreCase))
            {
                // Create a brand-new trace and link the span to the NSB sender span.
                // An activity without a parent context adopts Activity.Current as its parent when it
                // starts, so Current has to be cleared. See: https://github.com/dotnet/runtime/issues/65528#issuecomment-2613486896
                Activity.Current = null;
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
            // Create a span that will be a child of Activity.Current if available
            activity = activitySource.CreateActivity(activityName, ActivityKind.Consumer, parentContext: default);
        }

        if (activity is null)
        {
            return activity;
        }

        ContextPropagation.PropagateContextFromHeaders(activity, headers);

        activity.SetIdFormat(ActivityIdFormat.W3C);

        activity.AddTag(ActivityTags.NativeMessageId, nativeMessageId);
        ActivityDecorator.PromoteHeadersToTags(activity, headers);

        return activity;
    }

    public Activity? StartIncomingPipelineActivity(MessageContext context)
    {
        var activity = CreateActivityFromIncomingMessage(
            ActivitySources.Main,
            ActivityNames.IncomingMessageActivityName,
            context.Headers,
            context.NativeMessageId,
            context.Extensions);

        if (activity is null)
        {
            return activity;
        }

        activity.DisplayName = Options.UseMessageTypeNamesInSpanNames
            ? $"{ActivityDisplayNames.ProcessOperation} {context.ReceiveAddress}"
            : ActivityDisplayNames.ProcessMessage;

        activity.Start();

        return activity;
    }

    public Activity? StartOutgoingPipelineActivity(string activityName, string displayName, IBehaviorContext outgoingContext)
    {
        var activity = ActivitySources.Main.CreateActivity(activityName, ActivityKind.Producer);
        if (activity == null)
        {
            return activity;
        }

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
        var source = HandlerActivitySourceSwitch.UseHandlerActivitySource
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
        var activity = CreateActivityFromIncomingMessage(
            ActivitySources.Recoverability,
            ActivityNames.RecoverabilityActivityName,
            context.Headers,
            context.NativeMessageId,
            context.Extensions);

        if (activity is null)
        {
            return activity;
        }

        activity.DisplayName = ActivityDisplayNames.Recoverability;

        activity.Start();

        return activity;
    }

    public void UpdateActivityFromRecoverabilityAction(Activity activity, RecoverabilityAction recoverabilityAction, string receiveAddress)
    {
        if (recoverabilityAction is ImmediateRetry)
        {
            activity.AddTag(ActivityTags.RecoverabilityAction, "immediate_retry");
            activity.DisplayName = ActivityDisplayNames.ImmediateRetryOperation;

            if (Options.UseMessageTypeNamesInSpanNames)
            {
                activity.DisplayName += $" {receiveAddress}";
            }
        }
        else if (recoverabilityAction is DelayedRetry)
        {
            activity.AddTag(ActivityTags.RecoverabilityAction, "delayed_retry");
            activity.DisplayName = ActivityDisplayNames.DelayedRetryOperation;

            if (Options.UseMessageTypeNamesInSpanNames)
            {
                activity.DisplayName += $" {receiveAddress}";
            }
        }
        else if (recoverabilityAction is MoveToError moveToError)
        {
            activity.AddTag(ActivityTags.RecoverabilityAction, "move_to_error");

            activity.DisplayName = Options.UseMessageTypeNamesInSpanNames
                ? $"{ActivityDisplayNames.MoveToErrorOperation} {moveToError.ErrorQueue}"
                : $"{ActivityDisplayNames.MoveToErrorOperation} error";
        }
        else if (recoverabilityAction is Discard)
        {
            activity.AddTag(ActivityTags.RecoverabilityAction, "discard");
            activity.DisplayName = ActivityDisplayNames.DiscardOperation;
        }
    }

    public void RecordError(Activity? activity, Exception exception, ContextBag context)
    {
        if (activity == null)
        {
            return;
        }

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.SetTag(ActivityTags.ErrorType, exception.GetType().FullName);

        LegacyExceptionTags.SetLegacyStatusTags(activity, exception);

        if (!exception.Data.Contains(ExceptionRecordedFlag))
        {
            if (Options.ExceptionRecordingMode == ExceptionRecordingMode.Logs)
            {
                Logger.Error($"An exception occurred while executing '{activity.DisplayName}'.", exception);
            }
            else
            {
                activity.AddException(exception, LegacyExceptionTags.EscapedTagList);
            }

            exception.Data[ExceptionRecordedFlag] = true;
        }

        if (exception is TaskCanceledException)
        {
            activity.SetTag(ActivityTags.CancelledTask, true);
        }
    }

    const string ExceptionRecordedFlag = "otel.exception.recorded";

    static readonly ILog Logger = LogManager.GetLogger<ActivityFactory>();
}