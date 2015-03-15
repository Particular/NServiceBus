namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Sagas;

    class PopulateAutoCorrelationHeadersForRepliesBehavior : Behavior<OutgoingContext>
    {
        public override void Invoke(OutgoingContext context, Action next)
        {
            if (context.IsControlMessage())
            {
                next();
                return;
            }

            ActiveSagaInstance saga;

            if (context.TryGet(out saga) && HasBeenFound(saga))
            {
                context.Headers[Headers.OriginatingSagaId] = saga.SagaId;
                context.Headers[Headers.OriginatingSagaType] = saga.Metadata.SagaType.AssemblyQualifiedName;
            }

            TransportMessage incomingMessage;
            context.TryGet(TransportReceiveContext.IncomingPhysicalMessageKey, out incomingMessage);

            //auto correlate with the saga we are replying to if needed
            if (context.Headers[Headers.MessageIntent] == MessageIntentEnum.Reply.ToString()  && incomingMessage != null)
            {
                string sagaId;
                string sagaType;

                if (incomingMessage.Headers.TryGetValue(Headers.OriginatingSagaId, out sagaId))
                {
                    context.Headers[Headers.SagaId] = sagaId;
                }

                if (incomingMessage.Headers.TryGetValue(Headers.OriginatingSagaType, out sagaType))
                {
                    context.Headers[Headers.SagaType] = sagaType;
                }
            }

            next();
        }

        static bool HasBeenFound(ActiveSagaInstance saga)
        {
            return !saga.NotFound;
        }
    }
}