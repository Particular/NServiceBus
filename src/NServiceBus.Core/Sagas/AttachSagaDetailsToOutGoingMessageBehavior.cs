namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Pipeline.Contexts;
    using Sagas;
    using TransportDispatch;

    class AttachSagaDetailsToOutGoingMessageBehavior : Behavior<OutgoingLogicalMessageContext>
    {
        public override Task Invoke(OutgoingLogicalMessageContext context, Func<Task> next)
        {
            ActiveSagaInstance saga;

            //attach the current saga details to the outgoing headers for correlation
            if (context.TryGet(out saga) && HasBeenFound(saga) && !string.IsNullOrEmpty(saga.SagaId))
            {
                context.SetHeader(Headers.OriginatingSagaId, saga.SagaId);
                context.SetHeader(Headers.OriginatingSagaType, saga.Metadata.SagaType.AssemblyQualifiedName);
            }

            return next();
        }


        static bool HasBeenFound(ActiveSagaInstance saga)
        {
            return !saga.NotFound;
        }
    }
}