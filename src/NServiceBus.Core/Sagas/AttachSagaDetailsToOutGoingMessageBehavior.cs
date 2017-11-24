﻿namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Sagas;

    class AttachSagaDetailsToOutGoingMessageBehavior : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
    {
        public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
        {
            //attach the current saga details to the outgoing headers for correlation
            if (context.Extensions.TryGet(out ActiveSagaInstance saga) && HasBeenFound(saga) && !string.IsNullOrEmpty(saga.SagaId))
            {
                context.Headers[Headers.OriginatingSagaId] = saga.SagaId;
                context.Headers[Headers.OriginatingSagaType] = saga.Metadata.SagaType.AssemblyQualifiedName;
            }

            return next(context);
        }

        static bool HasBeenFound(ActiveSagaInstance saga)
        {
            return !saga.NotFound;
        }
    }
}