#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics;
using Extensibility;
using Pipeline;
using Transport;

interface IActivityFactory
{
    InstrumentationOptions Options { get; }
    Activity? StartIncomingPipelineActivity(MessageContext context);
    Activity? StartOutgoingPipelineActivity(string activityName, string displayName, IBehaviorContext outgoingContext);
    Activity? StartHandlerActivity(MessageHandler messageHandler);
    Activity? StartRecoverabilityActivity(ErrorContext context);
    void UpdateActivityFromRecoverabilityAction(Activity activity, RecoverabilityAction recoverabilityAction, string receiveAddress);
    // contextBag: the pipeline context of a failed message processing operation, which recoverability follows.
    // Omitted for a failure outside message processing.
    void RecordError(Activity? activity, Exception exception, IServiceProvider serviceProvider, ContextBag? contextBag = null);
}