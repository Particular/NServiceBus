namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Pipeline;

    class ReceiveDiagnosticsBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
    {

        public ReceiveDiagnosticsBehavior(string queueNameBase, string discriminator)
        {
            this.queueNameBase = queueNameBase;
            this.discriminator = discriminator;
        }

        public async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        {
            context.MessageHeaders.TryGetValue(Headers.EnclosedMessageTypes, out var messageTypes);

            var tags = new List<KeyValuePair<string, object>>
            {
                new("nservicebus.discriminator", discriminator ?? ""),
                new("nservicebus.queue", queueNameBase ?? ""),
                new("nservicebus.type", messageTypes ?? ""),
            };

            Meters.TotalFetched.Add(1, tags.ToArray());

            try
            {
                await next(context).ConfigureAwait(false);
            }
            catch (Exception ex) when (!ex.IsCausedBy(context.CancellationToken))
            {
                tags.Add(new KeyValuePair<string, object>("nservicebus.failure_type", ex.GetType()));
                Meters.TotalFailures.Add(1, tags.ToArray());
                throw;
            }

            Meters.TotalProcessedSuccessfully.Add(1, tags.ToArray());
        }

        readonly string queueNameBase;
        readonly string discriminator;
    }
}