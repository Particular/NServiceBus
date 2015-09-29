namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;

    class DispatchMessageToTransportConnector : StageConnector<PhysicalOutgoingContextStageBehavior.Context,DispatchContext>
    {
        public override Task Invoke(PhysicalOutgoingContextStageBehavior.Context context, Func<DispatchContext, Task> next)
        {
            var state = context.GetOrCreate<State>();
            state.Headers[Headers.MessageId] = state.MessageId;

            var message = new OutgoingMessage(state.MessageId, state.Headers, context.Body);
            
            return next(new DispatchContext(message,context));
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