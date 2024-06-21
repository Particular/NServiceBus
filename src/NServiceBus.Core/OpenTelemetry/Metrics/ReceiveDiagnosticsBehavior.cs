namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FastExpressionCompiler;
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
        var messageTypeHeader = !string.IsNullOrEmpty(messageTypes) ? messageTypes.Split(';').FirstOrDefault() : default;
        var messageTypeName = !string.IsNullOrEmpty(messageTypeHeader) ? messageTypeHeader.Split(',').FirstOrDefault() : default;

        var tags = new TagList(new KeyValuePair<string, object>[]
        {
            new(MeterTags.EndpointDiscriminator, discriminator ?? ""),
            new(MeterTags.QueueName, queueNameBase ?? ""),
            new(MeterTags.MessageType, messageTypeName ?? ""),
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