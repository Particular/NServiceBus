namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Logging;
    using Pipeline;
    using Serialization;
    using Unicast.Messages;

    class SerializeMessageConnector : StageConnector<IOutgoingLogicalMessageContext, IOutgoingPhysicalMessageContext>
    {
        public SerializeMessageConnector(IMessageSerializer messageSerializer, MessageMetadataRegistry messageMetadataRegistry)
        {
            this.messageSerializer = messageSerializer;
            this.messageMetadataRegistry = messageMetadataRegistry;
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
            context.Headers[Headers.EnclosedMessageTypes] = SerializeEnclosedMessageTypes(context.Message.MessageType);

            var stream = streamPool.Get();
            try
            {
                messageSerializer.Serialize(context.Message.Instance, stream);
                if (!stream.TryGetBuffer(out var buffer))
                {
                    throw new InvalidOperationException("Serialization stream buffer could not be acquired.");
                }

                var body = buffer.AsMemory(0, (int)stream.Position);
                await stage(this.CreateOutgoingPhysicalMessageContext(body, context.RoutingStrategies, context)).ConfigureAwait(false);
            }
            finally
            {
                streamPool.Return(stream);
            }
        }

        string SerializeEnclosedMessageTypes(Type messageType)
        {
            var metadata = messageMetadataRegistry.GetMessageMetadata(messageType);

            var assemblyQualifiedNames = new List<string>(metadata.MessageHierarchy.Length);
            foreach (var type in metadata.MessageHierarchy)
            {
                var typeAssemblyQualifiedName = type.AssemblyQualifiedName;
                if (assemblyQualifiedNames.Contains(typeAssemblyQualifiedName))
                {
                    continue;
                }

                assemblyQualifiedNames.Add(typeAssemblyQualifiedName);
            }

            return string.Join(";", assemblyQualifiedNames);
        }

        readonly MessageMetadataRegistry messageMetadataRegistry;
        readonly IMessageSerializer messageSerializer;
        readonly StreamPool streamPool = new StreamPool();

        static readonly ILog log = LogManager.GetLogger<SerializeMessageConnector>();
    }
}

