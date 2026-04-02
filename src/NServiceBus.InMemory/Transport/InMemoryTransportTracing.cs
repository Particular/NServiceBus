namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

static class InMemoryTransportTracing
{
    public const string ActivitySourceName = "NServiceBus.InMemory";

    public const string SendActivityName = "NServiceBus.InMemory.Send";
    public const string ScheduleActivityName = "NServiceBus.InMemory.Schedule";
    public const string ProcessActivityName = "NServiceBus.InMemory.Process";

    const string MessagingSystem = "messaging.system";
    const string DestinationName = "messaging.destination.name";
    const string OperationName = "messaging.operation.name";
    const string OperationType = "messaging.operation.type";
    const string MessageId = "messaging.message.id";
    const string ConversationId = "messaging.message.conversation_id";
    const string ErrorType = "error.type";
    const string InMemorySystemName = "inmemory";
    const string EnqueuedEventName = "inmemory.enqueued";
    const string ScheduledEventName = "inmemory.scheduled";
    const string HandoffEventName = "inmemory.handoff";

    static readonly ActivitySource activitySource = new(ActivitySourceName, "0.1.0");

    public static bool HasListeners() => activitySource.HasListeners();

    public static Activity? StartSend(string destination, string messageId, IReadOnlyDictionary<string, string> headers, bool delayed)
    {
        var operation = delayed ? "schedule" : "send";
        var activityName = delayed ? ScheduleActivityName : SendActivityName;
        var parentContext = ResolveProducerParentContext(headers);

        return StartActivity(activityName, ActivityKind.Producer, parentContext, destination, operation, "send", messageId, headers);
    }

    public static Activity? StartProcess(BrokerEnvelope envelope, string receiveAddress)
    {
        var parentContext = ResolveRemoteParentContext(envelope.Headers);
        var activity = StartActivity(ProcessActivityName, ActivityKind.Consumer, parentContext, receiveAddress, "process", "process", envelope.MessageId, envelope.Headers);

        PropagateContextFromHeaders(activity, envelope.Headers);
        activity?.AddEvent(new ActivityEvent(HandoffEventName));

        return activity;
    }

    public static void AddProducerDispatchEvent(Activity? activity, DateTimeOffset? deliverAt)
    {
        if (activity == null)
        {
            return;
        }

        if (deliverAt.HasValue)
        {
            activity.AddEvent(new ActivityEvent(ScheduledEventName, tags: new ActivityTagsCollection
            {
                ["message.deliver_at"] = deliverAt.Value.ToString("O")
            }));
            return;
        }

        activity.AddEvent(new ActivityEvent(EnqueuedEventName));
    }

    public static void MarkError(Activity? activity, Exception ex)
    {
        if (activity == null)
        {
            return;
        }

        activity.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity.SetTag("otel.status_code", "ERROR");
        activity.SetTag("otel.status_description", ex.Message);
        activity.AddEvent(new ActivityEvent("exception", DateTimeOffset.UtcNow,
        [
            new KeyValuePair<string, object?>("exception.escaped", true),
            new KeyValuePair<string, object?>("exception.type", ex.GetType()),
            new KeyValuePair<string, object?>("exception.message", ex.Message),
            new KeyValuePair<string, object?>("exception.stacktrace", ex.ToString())
        ]));
        activity.SetTag(ErrorType, ex.GetType().Name);
    }

    public static void MarkSuccess(Activity? activity)
    {
        if (activity == null)
        {
            return;
        }

        activity.SetStatus(ActivityStatusCode.Ok);
    }

    public static void PropagateContextToHeaders(Activity? activity, IDictionary<string, string> headers)
    {
        if (activity?.Id is not string activityId)
        {
            return;
        }

        headers[Headers.DiagnosticsTraceParent] = activityId;

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

    static Activity? StartActivity(string activityName, ActivityKind kind, ActivityContext parentContext, string destination, string operationName, string operationType, string messageId, IReadOnlyDictionary<string, string> headers)
    {
        var tags = new TagList
        {
            { MessagingSystem, InMemorySystemName },
            { DestinationName, destination },
            { OperationName, operationName },
            { OperationType, operationType },
            { MessageId, messageId }
        };

        if (headers.TryGetValue(Headers.ConversationId, out var conversationId))
        {
            tags.Add(ConversationId, conversationId);
        }

        var activity = activitySource.CreateActivity(activityName, kind, parentContext, tags, links: null, idFormat: ActivityIdFormat.W3C);
        if (activity == null)
        {
            return null;
        }

        activity.DisplayName = operationName;
        activity.Start();
        return activity;
    }

    static ActivityContext ResolveProducerParentContext(IReadOnlyDictionary<string, string> headers)
    {
        if (Activity.Current is { } currentActivity)
        {
            return currentActivity.Context;
        }

        return ResolveRemoteParentContext(headers);
    }

    static ActivityContext ResolveRemoteParentContext(IReadOnlyDictionary<string, string> headers)
    {
        if (headers.TryGetValue(Headers.DiagnosticsTraceParent, out var traceParent))
        {
            headers.TryGetValue(Headers.DiagnosticsTraceState, out var traceState);
            if (ActivityContext.TryParse(traceParent, traceState, isRemote: true, out var parentContext))
            {
                return parentContext;
            }
        }

        return default;
    }

    static void PropagateContextFromHeaders(Activity? activity, IReadOnlyDictionary<string, string> headers)
    {
        if (activity == null)
        {
            return;
        }

        if (headers.TryGetValue(Headers.DiagnosticsBaggage, out var baggageValue))
        {
            var baggageSpan = baggageValue.AsSpan();
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
