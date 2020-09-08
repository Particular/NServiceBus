namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;

    class PopulateAutoCorrelationHeadersForRepliesBehavior : IBehavior<IOutgoingReplyContext, IOutgoingReplyContext>
    {
        public Task Invoke(IOutgoingReplyContext context, Func<IOutgoingReplyContext, Task> next, CancellationToken cancellationToken)
        {
            FlowDetailsForRequestingSagaToOutgoingMessage(context, cancellationToken);

            return next(context);
        }

        static void FlowDetailsForRequestingSagaToOutgoingMessage(IOutgoingReplyContext context, CancellationToken cancellationToken)
        {
            if (context.TryGetIncomingPhysicalMessage(out var incomingMessage))
            {
                incomingMessage.Headers.TryGetValue(Headers.OriginatingSagaId, out var sagaId);
                incomingMessage.Headers.TryGetValue(Headers.OriginatingSagaType, out var sagaType);

                if (context.Extensions.TryGet(out State state))
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