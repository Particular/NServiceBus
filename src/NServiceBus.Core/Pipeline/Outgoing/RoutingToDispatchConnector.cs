using System.Collections.Generic;
using NServiceBus.DelayedDelivery;
using NServiceBus.Performance.TimeToBeReceived;

namespace NServiceBus
{
    using System;
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

            var operations = new TransportOperation[context.RoutingStrategies.Count];
            var index = 0;
            foreach (var strategy in context.RoutingStrategies)
            {
                var addressLabel = strategy.Apply(context.Message.Headers);
                var message = new OutgoingMessage(context.Message.MessageId, context.Message.Headers, context.Message.Body);
                var deliverConstraints = context.Extensions.GetDeliveryConstraints();
                var properties = new Dictionary<string, string>();
                foreach (var deliveryConstraint in deliverConstraints)
                {
                    switch (deliveryConstraint)
                    {
                        case DelayDeliveryWith delayWith:
                            properties.Add(typeof(DelayDeliveryWith).FullName, delayWith.Delay.ToString("c"));
                            break;
                        case DoNotDeliverBefore doNotDeliverBefore:
                            properties.Add(typeof(DoNotDeliverBefore).FullName, doNotDeliverBefore.At.ToString("O"));
                            break;
                        case DiscardIfNotReceivedBefore ttbr:
                            properties.Add(typeof(DiscardIfNotReceivedBefore).FullName, ttbr.MaxTime.ToString("c"));
                            break;
                    }
                }
                operations[index] = new TransportOperation(message, addressLabel, properties, dispatchConsistency);
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