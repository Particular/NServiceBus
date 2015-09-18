namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Serialization;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Unicast.Messages;

    class SerializeMessagesBehavior : StageConnector<OutgoingContext, PhysicalOutgoingContextStageBehavior.Context>
    {
        IMessageSerializer messageSerializer;
        MessageMetadataRegistry messageMetadataRegistry;

        public SerializeMessagesBehavior(IMessageSerializer messageSerializer, MessageMetadataRegistry messageMetadataRegistry)
        {
            this.messageSerializer = messageSerializer;
            this.messageMetadataRegistry = messageMetadataRegistry;
        }

        public override async Task Invoke(OutgoingContext context, Func<PhysicalOutgoingContextStageBehavior.Context, Task> next)
        {
            if (context.GetOrCreate<State>().SkipSerialization)
            {
                await next(new PhysicalOutgoingContextStageBehavior.Context(new byte[0], context)).ConfigureAwait(false);
                return;
            }

            context.SetHeader(Headers.ContentType, messageSerializer.ContentType);
            context.SetHeader(Headers.EnclosedMessageTypes, SerializeEnclosedMessageTypes(context.GetMessageType()));

            var array = Serialize(context);
            await next(new PhysicalOutgoingContextStageBehavior.Context(array, context)).ConfigureAwait(false);
        }

        byte[] Serialize(OutgoingContext context)
        {
            using (var ms = new MemoryStream())
            {
                messageSerializer.Serialize(context.GetMessageInstance(), ms);
                return ms.ToArray();
            }
        }

        string SerializeEnclosedMessageTypes(Type messageType)
        {
            var metadata = messageMetadataRegistry.GetMessageMetadata(messageType);
            var distinctTypes = metadata.MessageHierarchy.Distinct();
            return string.Join(";", distinctTypes.Select(t => t.AssemblyQualifiedName));
        }

        public class State
        {
            public bool SkipSerialization { get; set; }
        }

    }

    /// <summary>
    /// Allows users to control serialization.
    /// </summary>
    public static class SerializationContextExtensions
    {
        /// <summary>
        /// Requests the serializer to skip serializing the message.
        /// </summary>
        public static void SkipSerialization(this OutgoingContext context)
        {
            context.GetOrCreate<SerializeMessagesBehavior.State>().SkipSerialization = true;
        }
    }
}