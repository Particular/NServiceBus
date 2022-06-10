namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using System.Threading.Tasks;
    using Extensibility;
    using Logging;
    using Pipeline;
    using Routing;
    using Transport;

    class RoutingToDispatchConnector : StageConnector<IRoutingContext, IDispatchContext>
    {
        public override Task Invoke(IRoutingContext context, Func<IDispatchContext, Task> stage)
        {
            var dispatchConsistency = DispatchConsistency.Default;
            if (context.GetOperationProperties().TryGet(out State state) && state.ImmediateDispatch)
            {
                dispatchConsistency = DispatchConsistency.Isolated;
            }

            // HINT: Context is propagated to the message headers from the current activity, if present.
            // This may not be the outgoing message activity created by NServiceBus.
            ContextPropagation.PropagateContextToHeaders(Activity.Current, context.Message.Headers);

            var operations = new TransportOperation[context.RoutingStrategies.Count];
            var index = 0;
            foreach (var strategy in context.RoutingStrategies)
            {
                operations[index] = context.ToTransportOperation(strategy, dispatchConsistency);
                index++;
            }

            if (isDebugEnabled)
            {
                LogOutgoingOperations(operations);
            }

            // HINT: These tags get applied to the outgoing message activity, if present.
            if (context.Extensions.TryGet<Activity>(DiagnosticsKeys.OutgoingActivityKey, out var activity))
            {
                ActivityDecorator.SetOutgoingTraceTags(activity, context.Message, operations);
                ActivityDecorator.SetHeaderTraceTags(activity, context.Message.Headers);
            }

            if (dispatchConsistency == DispatchConsistency.Default && context.Extensions.TryGet(out PendingTransportOperations pendingOperations))
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