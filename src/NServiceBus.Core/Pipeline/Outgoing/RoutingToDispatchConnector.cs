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

            AddTraceInfo(Activity.Current, operations);

            if (dispatchConsistency == DispatchConsistency.Default && context.Extensions.TryGet(out PendingTransportOperations pendingOperations))
            {
                pendingOperations.AddRange(operations);
                return Task.CompletedTask;
            }

            return stage(this.CreateDispatchContext(operations, context));
        }

        static void AddTraceInfo(Activity activity, TransportOperation[] operations)
        {
            if (activity == null)
            {
                return;
            }

            // TODO: How do we handle multiple operations here?
            foreach (var operation in operations)
            {
                activity.AddTag("messaging.message_id", operation.Message.MessageId);
                activity.AddTag("messaging.operation", "send");

                if (operation.AddressTag is UnicastAddressTag unicastAddressTag)
                {
                    activity.AddTag("messaging.destination", unicastAddressTag.Destination);
                    activity.AddTag("messaging.destination_kind", "queue");
                    activity.DisplayName = $"{unicastAddressTag.Destination} send";
                }

                // TODO: Multicast address tags to topics

                if (operation.Message.Headers.TryGetValue(Headers.ConversationId, out var conversationId))
                {
                    activity.AddTag("messaging.conversation_id", conversationId);
                }

                // HINT: This needs to be converted into a string or the tag is not created
                activity.AddTag("messaging.message_payload_size_bytes", operation.Message.Body.Length.ToString());
            }
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