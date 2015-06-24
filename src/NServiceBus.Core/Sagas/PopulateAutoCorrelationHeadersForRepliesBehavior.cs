namespace NServiceBus
{
    using System;
    using NServiceBus.Routing;
    using NServiceBus.Saga;
    using NServiceBus.TransportDispatch;
    using Pipeline;
    using Pipeline.Contexts;

    class PopulateAutoCorrelationHeadersForRepliesBehavior : Behavior<OutgoingContext>
    {
        public override void Invoke(OutgoingContext context, Action next)
        {
            AttachSagaDetailsToOutGoingMessage(context);

            FlowDetailsForRequestingSagaToOutgoingMessage(context);

            next();
        }

        static void FlowDetailsForRequestingSagaToOutgoingMessage(OutgoingContext context)
        {
            TransportMessage incomingMessage;

            if (context.TryGetIncomingPhysicalMessage(out incomingMessage))
            {
                //flow the the saga id of the calling saga (if any) to outgoing message in order to support autocorrelation
                if (context.IsReply())
                {

                    string sagaId;

                    incomingMessage.Headers.TryGetValue(Headers.OriginatingSagaId, out sagaId);

                    string sagaType;

                    incomingMessage.Headers.TryGetValue(Headers.OriginatingSagaType, out sagaType);

                    State state;

                    if (context.Extensions.TryGet(out state))
                    {
                        sagaId = state.SagaIdToUse;
                        sagaType = state.SagaTypeToUse;
                    }

                    if (!string.IsNullOrEmpty(sagaId))
                    {
                        context.SetHeader(Headers.SagaId, sagaId);
                    }

                    if (!string.IsNullOrEmpty(sagaType))
                    {
                        context.SetHeader(Headers.SagaType, sagaType);
                    }
                }
            }
        }

        static void AttachSagaDetailsToOutGoingMessage(OutgoingContext context)
        {
            ActiveSagaInstance saga;


            //attach the current saga details to the outgoing headers for correlation
            if (context.TryGet(out saga) && HasBeenFound(saga))
            {
                context.SetHeader(Headers.OriginatingSagaId, saga.SagaId);
                context.SetHeader(Headers.OriginatingSagaType, saga.Metadata.SagaType.AssemblyQualifiedName);
            }
        }

        static bool HasBeenFound(ActiveSagaInstance saga)
        {
            return !saga.NotFound;
        }


        public class State
        {
            public string SagaIdToUse { get; set; }

            public string SagaTypeToUse { get; set; }
        }
    }
}