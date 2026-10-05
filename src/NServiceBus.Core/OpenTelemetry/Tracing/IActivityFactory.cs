#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics;
using Pipeline;
using Transport;

interface IActivityFactory
{
    InstrumentationOptions Options { get; }
    Activity? StartIncomingPipelineActivity(MessageContext context);
    Activity? StartOutgoingPipelineActivity(string activityName, string displayName, IBehaviorContext outgoingContext);

    // The display name is only built when a span is recorded: with the v11 behavior it is "{operation} {message type}",
    // before v11 it is legacyDisplayName. Avoids a string allocation per operation when nobody listens.
    Activity? StartOutgoingPipelineActivity(string activityName, string legacyDisplayName, string operation, Type messageType, IBehaviorContext outgoingContext);
    Activity? StartOutgoingPipelineActivity(string activityName, string legacyDisplayName, string operation, Type[] messageTypes, IBehaviorContext outgoingContext);
    Activity? StartHandlerActivity(MessageHandler messageHandler);
    Activity? StartRecoverabilityActivity(ErrorContext context);
    void UpdateActivityFromRecoverabilityAction(Activity activity, RecoverabilityAction recoverabilityAction, string receiveAddress);
    void RecordError(Activity? activity, Exception exception, IServiceProvider serviceProvider);
}