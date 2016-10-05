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
        public override Task Invoke(IRoutingContext context, Func<IDispatchContext, Task> stage)
        {
            var state = context.Extensions.GetOrCreate<State>();
            var dispatchConsistency = state.ImmediateDispatch ? DispatchConsistency.Isolated : DispatchConsistency.Default;

            var operations = context.RoutingStrategies
                .Select(rs =>
                {
                    var addressLabel = rs.Apply(context.Message.Headers);
                    var message = new OutgoingMessage(context.Message.MessageId, context.Message.Headers, context.Message.Body);
                    return new TransportOperation(message, addressLabel, dispatchConsistency, context.Extensions.GetDeliveryConstraints());
                }).ToArray();

            if (log.IsDebugEnabled)
            {
                var sb = new StringBuilder();
                foreach (var operation in operations)
                {
                    var unicastAddressTag = operation.AddressTag as UnicastAddressTag;
                    if (unicastAddressTag != null)
                    {
                        sb.AppendFormat("Destination: {0}\n", unicastAddressTag.Destination);
                    }

                    sb.AppendFormat("Message headers:\n{0}", string.Join(", ", operation.Message.Headers.Select(h => h.Key + ":" + h.Value).ToArray()));
                    log.Debug(sb.ToString());
                }
            }

            PendingTransportOperations pendingOperations;

            if (!state.ImmediateDispatch && context.Extensions.TryGet(out pendingOperations))
            {
                pendingOperations.AddRange(operations);
                return TaskEx.CompletedTask;
            }

            return stage(this.CreateDispatchContext(operations, context));
        }

        static ILog log = LogManager.GetLogger<RoutingToDispatchConnector>();

        public class State
        {
            public bool ImmediateDispatch { get; set; }
        }
    }
}