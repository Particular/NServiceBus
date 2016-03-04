namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;

    class FaultToDispatchConnector : StageConnector<IFaultContext, IRoutingContext>
    {
        public override Task Invoke(IFaultContext context, Func<IRoutingContext, Task> stage)
        {
            var message = context.Message;

            State state;

            if (context.Extensions.TryGet(out state))
            {
                //transfer fault values to the headers of the message to fault
                foreach (var kvp in state.FaultyValues)
                {
                    message.Headers[kvp.Key] = kvp.Value;
                }
            }

            var dispatchContext = this.CreateRoutingContext(context.Message, new UnicastRoutingStrategy(context.ErrorQueueAddress), context);

            return stage(dispatchContext);
        }

        public class State
        {
            public Dictionary<string, string> FaultyValues = new Dictionary<string, string>();
        }
    }
}