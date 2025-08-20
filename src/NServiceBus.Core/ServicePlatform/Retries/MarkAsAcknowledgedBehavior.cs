﻿#nullable enable

namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Pipeline;

class MarkAsAcknowledgedBehavior : IBehavior<IAuditContext, IAuditContext>
{
    public Task Invoke(IAuditContext context, Func<IAuditContext, Task> next)
    {
        if (context.Extensions.TryGet<State>(out _))
        {
            context.AuditMetadata["ServiceControl.Retry.AcknowledgementSent"] = "true";
        }

        return next(context);
    }

    public class State
    {
        public static readonly State Instance = new();

        State()
        {
        }
    }
}