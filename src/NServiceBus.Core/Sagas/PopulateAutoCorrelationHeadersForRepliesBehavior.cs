namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

    class PopulateAutoCorrelationHeadersForRepliesBehavior : Behavior<IOutgoingReplyContext>
    {
        public override Task Invoke(IOutgoingReplyContext context, Func<Task> next)
        {
            FlowDetailsForRequestingSagaToOutgoingMessage(context);

            return next();
        }

        static void FlowDetailsForRequestingSagaToOutgoingMessage(IOutgoingReplyContext context)
        {
            IncomingMessage incomingMessage;

            if (context.TryGetIncomingPhysicalMessage(out incomingMessage))
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
                    context.Headers[Headers.SagaId] = sagaId;
                }

                if (!string.IsNullOrEmpty(sagaType))
                {
                    context.Headers[Headers.SagaType] = sagaType;
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