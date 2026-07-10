#nullable enable

namespace NServiceBus;

using System.Diagnostics;
using Pipeline;
using Transport;

sealed class NoOpActivityFactory : IActivityFactory
{
    public static NoOpActivityFactory Instance = new();
    public InstrumentationOptions Options { get; } = new();

    public Activity? StartIncomingPipelineActivity(MessageContext context) => null;

    public Activity? StartOutgoingPipelineActivity(string activityName, string displayName, IBehaviorContext outgoingContext) => null;

    public Activity? StartHandlerActivity(MessageHandler messageHandler) => null;
    public Activity? StartRecoverabilityActivity(ErrorContext context) => null;
}