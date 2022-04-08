namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using Pipeline;

    class PopulateAutoCorrelationHeadersForRepliesBehavior : IBehavior<IOutgoingReplyContext, IOutgoingReplyContext>
    {
        public Task Invoke(IOutgoingReplyContext context, Func<IOutgoingReplyContext, Task> next)
        {
            FlowDetailsForRequestingSagaToOutgoingMessage(context);

            return next(context);
        }

        static void FlowDetailsForRequestingSagaToOutgoingMessage(IOutgoingReplyContext context)
        {
            if (context.TryGetIncomingPhysicalMessage(out var incomingMessage))
            {
                incomingMessage.Headers.TryGetValue(Headers.OriginatingSagaId, out var sagaId);
                incomingMessage.Headers.TryGetValue(Headers.OriginatingSagaType, out var sagaType);

                if (context.Extensions.TryGet(ContextBag.GetPrefixedKey<State>(context.MessageId), out State state))
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