#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;

static class ReceivedMessageTypes
{
    public static IEnumerable<Type> GetHandledEventTypes(this IEnumerable<Type> messageTypes, Conventions conventions) =>
        messageTypes
            .Where(t => !conventions.IsInSystemConventionList(t)) //never auto-subscribe system messages
            .Where(t => !conventions.IsCommandType(t)) //commands should never be subscribed to
            .Where(t => conventions.IsEventType(t)); //only events
}
