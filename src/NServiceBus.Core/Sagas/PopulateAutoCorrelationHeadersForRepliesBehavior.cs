namespace NServiceBus
{
    using System;
    using NServiceBus.OutgoingPipeline.Reply;
    using NServiceBus.Pipeline;
    using NServiceBus.TransportDispatch;

    class PopulateAutoCorrelationHeadersForRepliesBehavior : Behavior<OutgoingReplyContext>
    {
        public override void Invoke(OutgoingReplyContext context, Action next)
        {
            FlowDetailsForRequestingSagaToOutgoingMessage(context);

            next();
        }

        static void FlowDetailsForRequestingSagaToOutgoingMessage(OutgoingReplyContext context)
        {
            TransportMessage incomingMessage;

            if (context.TryGetIncomingPhysicalMessage(out incomingMessage))
            {

                string sagaId;

                incomingMessage.Headers.TryGetValue(Headers.OriginatingSagaId, out sagaId);

                string sagaType;

                incomingMessage.Headers.TryGetValue(Headers.OriginatingSagaType, out sagaType);

                State state;

                if (context.TryGet(out state))
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

        public class State
        {
            public string SagaIdToUse { get; set; }

            public string SagaTypeToUse { get; set; }
        }
    }
}