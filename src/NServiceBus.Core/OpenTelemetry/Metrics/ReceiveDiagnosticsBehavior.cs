namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Pipeline;

    class ReceiveDiagnosticsBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
    {
        readonly Lazy<bool> hasMetricsListener = new Lazy<bool>(() =>
            Meters.TotalProcessedSuccessfully.Enabled || Meters.TotalFetched.Enabled || Meters.TotalFailures.Enabled);

        public ReceiveDiagnosticsBehavior(string queueNameBase, string discriminator)
        {
            this.queueNameBase = queueNameBase;
            this.discriminator = discriminator;
        }

        public Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        {
            if (!hasMetricsListener.Value)
            {
                return next(context);
            }

            return InvokeAndCaptureMetrics(context, next);
        }

        async Task InvokeAndCaptureMetrics(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        {
            context.MessageHeaders.TryGetValue(Headers.EnclosedMessageTypes, out var messageTypes);

            var tags = new TagList(new KeyValuePair<string, object>[]
            {
                new(MeterTags.EndpointDiscriminator, discriminator ?? ""),
                new(MeterTags.QueueName, queueNameBase ?? ""),
                new(MeterTags.MessageType, messageTypes ?? ""),
            }.AsSpan());

            Meters.TotalFetched.Add(1, tags);

            try
            {
                await next(context).ConfigureAwait(false);
            }
            catch (Exception ex) when (!ex.IsCausedBy(context.CancellationToken))
            {
                tags.Add(new KeyValuePair<string, object>(MeterTags.FailureType, ex.GetType()));
                Meters.TotalFailures.Add(1, tags);
                throw;
            }

            Meters.TotalProcessedSuccessfully.Add(1, tags);
        }

        readonly string queueNameBase;
        readonly string discriminator;
    }
}