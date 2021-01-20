namespace NServiceBus
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
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

            var operations = new TransportOperation[context.RoutingStrategies.Count];
            var index = 0;
            foreach (var strategy in context.RoutingStrategies)
            {
                var addressLabel = strategy.Apply(context.Message.Headers);
                var message = new OutgoingMessage(context.Message.MessageId, context.Message.Headers, context.Message.Body);

                if (!context.Extensions.TryGet(out DispatchProperties dispatchProperties))
                {
                    dispatchProperties = new DispatchProperties();
                }

                operations[index] = new TransportOperation(message, addressLabel, dispatchProperties, dispatchConsistency);
                index++;
            }

            if (isDebugEnabled)
            {
                LogOutgoingOperations(operations);
            }

            if (!state.ImmediateDispatch && context.Extensions.TryGet(out PendingTransportOperations pendingOperations))
            {
                pendingOperations.AddRange(operations);
                return Task.CompletedTask;
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
                    sb.AppendLine($"Destination: {unicastAddressTag.Destination}");
                }

                sb.AppendLine("Message headers:");

                foreach (var kvp in operation.Message.Headers)
                {
                    sb.AppendLine($"{kvp.Key} : {kvp.Value}");
                }
            }

            log.Debug(sb.ToString());
        }

        static readonly ILog log = LogManager.GetLogger<RoutingToDispatchConnector>();
        static readonly bool isDebugEnabled = log.IsDebugEnabled;

        public class State
        {
            public bool ImmediateDispatch { get; set; }
        }
    }
}