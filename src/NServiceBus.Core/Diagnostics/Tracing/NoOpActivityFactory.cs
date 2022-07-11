namespace NServiceBus;

using System.Diagnostics;
using Pipeline;
using Sagas;
using Transport;

class NoOpActivityFactory : IActivityFactory
{
    public Activity StartIncomingActivity(MessageContext context) => null;

    public Activity StartOutgoingPipelineActivity(string activityName, string displayName, IBehaviorContext outgoingContext) => null;

    public Activity StartHandlerActivity(MessageHandler messageHandler, ActiveSagaInstance saga) => null;
}