namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.TransportDispatch;

    class PopulateAutoCorrelationHeadersForRepliesBehavior : Behavior<OutgoingReplyContext>
    {
        public override Task Invoke(OutgoingContext context, Func<Task> next)
        {
            FlowDetailsForRequestingSagaToOutgoingMessage(context);

            return next();
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