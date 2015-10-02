namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using OutgoingPipeline;
    using Pipeline;
    using TransportDispatch;
    using Transports;

    class OutgoingPhysicalToRoutingConnector : StageConnector<OutgoingPhysicalMessageContext, RoutingContext>
    {
        public override Task Invoke(OutgoingPhysicalMessageContext context, Func<RoutingContext, Task> next)
        {
            var state = context.GetOrCreate<State>();
            state.Headers[Headers.MessageId] = state.MessageId;

            var message = new OutgoingMessage(state.MessageId, state.Headers, context.Body);

            return next(new RoutingContext(message, context.RoutingStrategies, context));
        }

        public class State
        {
            public State()
            {
                Headers = new Dictionary<string, string>();
                MessageId = CombGuid.Generate().ToString();
            }
            public Dictionary<string, string> Headers { get; }
            public string MessageId { get; set; }
        }
    }
}