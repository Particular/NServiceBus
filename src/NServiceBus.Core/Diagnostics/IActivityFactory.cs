﻿namespace NServiceBus;

using System.Diagnostics;
using Pipeline;
using Sagas;
using Transport;

interface IActivityFactory
{
    Activity StartIncomingActivity(MessageContext context);
    Activity StartOutgoingPipelineActivity(string activityName, string displayName, IBehaviorContext outgoingContext);
    Activity StartHandlerActivity(MessageHandler messageHandler, ActiveSagaInstance saga);
}