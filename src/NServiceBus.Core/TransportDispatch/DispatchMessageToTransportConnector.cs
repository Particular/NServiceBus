namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;

    class DispatchMessageToTransportConnector : StageConnector<PhysicalOutgoingContextStageBehavior.Context,DispatchContext>
    {
        public override void Invoke(PhysicalOutgoingContextStageBehavior.Context context, Action<DispatchContext> next)
        {
            var state = context.GetOrCreate<State>();
            state.Headers[Headers.MessageId] = state.MessageId;

            var message = new OutgoingMessage(state.MessageId, state.Headers, context.Body);
            
            
            next(new DispatchContext(message,context));
        }
     
        public class State
        {
            public State()
            {
                Headers = new Dictionary<string, string>();
                MessageId = CombGuid.Generate().ToString();
            }
            public Dictionary<string, string> Headers { get; private set; }
            public string MessageId { get; set; }
        }
    }
}