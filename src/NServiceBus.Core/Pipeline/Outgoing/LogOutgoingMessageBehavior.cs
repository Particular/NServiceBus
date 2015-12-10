﻿namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.OutgoingPipeline;
    using NServiceBus.Routing;

    class LogOutgoingMessageBehavior : Behavior<DispatchContextImpl>
    {
        public override async Task Invoke(DispatchContextImpl context, Func<Task> next)
        {
            var outgoingLogicalMessageContext = context.Get<OutgoingLogicalMessageContext>();
            
            if (log.IsDebugEnabled && outgoingLogicalMessageContext != null && outgoingLogicalMessageContext.Message != null)
            {
                var sb = new StringBuilder();

                sb.AppendFormat("Dispatching message '{0}' with id '{1}'. Routing Details:\n",
                    outgoingLogicalMessageContext.Message.MessageType != null ? outgoingLogicalMessageContext.Message.MessageType.AssemblyQualifiedName : "unknown",
                    outgoingLogicalMessageContext.MessageId);

                foreach (var transportOperation in context.Operations)
                {
                    var unicastAddressTag = transportOperation.DispatchOptions.AddressTag as UnicastAddressTag;
                    if (unicastAddressTag != null)
                        sb.AppendFormat("Destination: {0}\n", unicastAddressTag.Destination);
                    sb.AppendFormat("ToString() of the message yields: {0}\n" +
                                    "Message headers:\n{1}", outgoingLogicalMessageContext.Message.Instance, string.Join(", ", transportOperation.Message.Headers.Select(h => h.Key + ":" + h.Value).ToArray()));
                }

                log.Debug(sb.ToString());
            }

            await next().ConfigureAwait(false);
        }

        static ILog log = LogManager.GetLogger("LogOutgoingMessage");
    }
}