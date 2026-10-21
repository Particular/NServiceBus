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
    // Marks the activity as failed and, once per exception, records the exception on the innermost activity it
    // escaped from. Logging the exception is the caller's concern.
    void RecordError(Activity? activity, Exception exception);

    // A failure in the message processing pipeline: recoverability follows, so the innermost pipeline activity the
    // exception escaped from is also recorded on the context bag for the recoverability activity to link to.
    void RecordError(Activity? activity, Exception exception, ContextBag contextBag);
}