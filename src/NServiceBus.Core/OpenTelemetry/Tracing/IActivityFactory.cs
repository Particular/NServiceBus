#nullable enable

namespace NServiceBus;

using System.Diagnostics;
using Pipeline;
using Transport;

interface IActivityFactory
{
    Activity? StartIncomingPipelineActivity(MessageContext context);
    Activity? StartOutgoingPipelineActivity(string activityName, string displayName, IBehaviorContext outgoingContext);
    Activity? StartHandlerActivity(MessageHandler messageHandler);
}