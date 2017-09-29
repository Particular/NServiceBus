namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using DeliveryConstraints;
    using Logging;
    using Pipeline;
    using Routing;
    using Transport;

    class RoutingToDispatchConnector : StageConnector<IRoutingContext, IDispatchContext>
    {
        public RoutingToDispatchConnector(string localAddress)
        {
            this.localAddress = localAddress;
        }
        
        public override Task Invoke(IRoutingContext context, Func<IDispatchContext, Task> stage)
        {
            var state = context.Extensions.GetOrCreate<State>();
            var dispatchConsistency = state.ImmediateDispatch ? DispatchConsistency.Isolated : DispatchConsistency.Default;

            var operations = new TransportOperation[context.RoutingStrategies.Count];
            var index = 0;
            foreach (var strategy in context.RoutingStrategies)
            {
                var appliedStrategy = strategy;
                if (appliedStrategy is RouteToThisEndpointStrategy)
                {
                    // hack until we get rid of HandleCurrentMessageLater
                    appliedStrategy = new UnicastRoutingStrategy(localAddress);
                }
                
                var addressLabel = appliedStrategy.Apply(context.Message.Headers);
                var message = new OutgoingMessage(context.Message.MessageId, context.Message.Headers, context.Message.Body);
                operations[index] = new TransportOperation(message, addressLabel, dispatchConsistency, context.Extensions.GetDeliveryConstraints());
                index++;
            }

            if (isDebugEnabled)
            {
                LogOutgoingOperations(operations);
            }

            if (!state.ImmediateDispatch && context.Extensions.TryGet(out PendingTransportOperations pendingOperations))
            {
                pendingOperations.AddRange(operations);
                return TaskEx.CompletedTask;
            }

            return stage(this.CreateDispatchContext(operations, context));
        }

        static void LogOutgoingOperations(TransportOperation[] operations)
        {
            var sb = new StringBuilder();
            foreach (var operation in operations)
            {
                if (operation.AddressTag is UnicastAddressTag unicastAddressTag)
                {
                    sb.AppendFormat("Destination: {0}" + Environment.NewLine, unicastAddressTag.Destination);
                }

                sb.AppendFormat("Message headers:" + Environment.NewLine + "{0}", string.Join(", ", operation.Message.Headers.Select(h => h.Key + ":" + h.Value).ToArray()));
            }
            log.Debug(sb.ToString());
        }

        static ILog log = LogManager.GetLogger<RoutingToDispatchConnector>();
        static readonly bool isDebugEnabled = log.IsDebugEnabled;
        string localAddress;

        public class State
        {
            public bool ImmediateDispatch { get; set; }
        }
    }
}