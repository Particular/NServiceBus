#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics;
using System.Threading;
using Extensibility;
using Pipeline;
using Transport;

interface IActivityFactory
{
    Activity? StartIncomingPipelineActivity(MessageContext context);
    Activity? StartOutgoingPipelineActivity(string activityName, string displayName, IBehaviorContext outgoingContext);
    Activity? StartHandlerActivity(MessageHandler messageHandler);
    Activity? StartRecoverabilityActivity(ErrorContext context);
    void EnrichActivityWithRecoverabilityActionData(Activity activity, RecoverabilityAction recoverabilityAction, string receiveAddress);
#pragma warning disable PS0003
    void RecordError(Activity? activity, Exception exception, ContextBag contextBag, CancellationToken cancellationToken);
#pragma warning restore PS0003
}