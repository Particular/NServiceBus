#nullable enable

namespace NServiceBus;

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
        if (context.GetOperationProperties().TryGet<State>(out var state) && state.ImmediateDispatch)
        {
            dispatchConsistency = DispatchConsistency.Isolated;
        }

        // HINT: Context is propagated to the message headers from the current activity, if present.
        // This may not be the outgoing message activity created by NServiceBus.
        ContextPropagation.PropagateContextToHeaders(Activity.Current, context.Message.Headers, context.Extensions);

        var operations = new TransportOperation[context.RoutingStrategies.Count];
        var index = 0;
        // when there are more than one routing strategy we want to make sure each transport operation is independent
        var copySharedMutableMessageState = context.RoutingStrategies.Count > 1;
        foreach (var strategy in context.RoutingStrategies)
        {
            operations[index] = context.ToTransportOperation(strategy, dispatchConsistency, copySharedMutableMessageState);
            index++;
        }

        if (isDebugEnabled)
        {
            LogOutgoingOperations(operations);
        }

        // HINT: These tags get applied to the outgoing message activity, if present.
        if (context.Extensions.TryGetRecordingOutgoingPipelineActivity(out var activity))
        {
            ActivityDecorator.PromoteHeadersToTags(activity, context.Message.Headers);
        }

        if (dispatchConsistency == DispatchConsistency.Default && context.Extensions.TryGet<PendingTransportOperations>(out var pendingOperations))
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