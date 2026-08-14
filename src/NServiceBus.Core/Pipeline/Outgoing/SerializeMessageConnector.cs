#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Logging;
using Pipeline;
using Serialization;
using Unicast.Messages;

class SerializeMessageConnector : StageConnector<IOutgoingLogicalMessageContext, IOutgoingPhysicalMessageContext>
{
    public SerializeMessageConnector(IMessageSerializer messageSerializer, MessageMetadataRegistry messageMetadataRegistry, IncomingPipelineMetrics incomingPipelineMetrics)
    {
        this.messageSerializer = messageSerializer;
        this.messageMetadataRegistry = messageMetadataRegistry;
        this.incomingPipelineMetrics = incomingPipelineMetrics;
    }

    public override async Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingPhysicalMessageContext, Task> stage)
    {
        if (log.IsDebugEnabled)
        {
            log.DebugFormat("Serializing message '{0}' with id '{1}', ToString() of the message yields: {2}",
                context.Message.MessageType != null ? context.Message.MessageType.AssemblyQualifiedName : "unknown",
                context.MessageId, context.Message.Instance);
        }

        if (context.ShouldSkipSerialization())
        {
            await stage(this.CreateOutgoingPhysicalMessageContext(ReadOnlyMemory<byte>.Empty, context.RoutingStrategies, context)).ConfigureAwait(false);
            return;
        }

        context.Headers[Headers.ContentType] = messageSerializer.ContentType;
        var metadata = messageMetadataRegistry.GetMessageMetadata(context.Message.MessageType);
        context.Headers[Headers.EnclosedMessageTypes] = metadata.MessageHierarchySerialized;

        using (var ms = new MemoryStream())
        {
            var serializeStart = Stopwatch.GetTimestamp();
            try
            {
                messageSerializer.Serialize(context.Message.Instance, ms);
                incomingPipelineMetrics.RecordSerializeTime(Stopwatch.GetElapsedTime(serializeStart), context.Message.MessageType?.FullName);
            }
#pragma warning disable PS0019
            catch (Exception ex)
#pragma warning restore PS0019
            {
                incomingPipelineMetrics.RecordSerializeTime(Stopwatch.GetElapsedTime(serializeStart), context.Message.MessageType?.FullName, error: ex);
                throw;
            }

            var body = ms.GetBuffer().AsMemory(0, (int)ms.Position);

            await stage(this.CreateOutgoingPhysicalMessageContext(body, context.RoutingStrategies, context)).ConfigureAwait(false);
        }
    }

    readonly MessageMetadataRegistry messageMetadataRegistry;
    readonly IMessageSerializer messageSerializer;
    readonly IncomingPipelineMetrics incomingPipelineMetrics;

    static readonly ILog log = LogManager.GetLogger<SerializeMessageConnector>();
}
