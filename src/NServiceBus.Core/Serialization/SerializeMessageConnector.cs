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

    //todo: rename to LogicalOutgoingContext
    class SerializeMessageConnector : StageConnector<OutgoingLogicalMessageContext, OutgoingPhysicalMessageContext>
    {
        IMessageSerializer messageSerializer;
        MessageMetadataRegistry messageMetadataRegistry;

        public SerializeMessageConnector(IMessageSerializer messageSerializer, MessageMetadataRegistry messageMetadataRegistry)
        {
            this.messageSerializer = messageSerializer;
            this.messageMetadataRegistry = messageMetadataRegistry;
        }

        public override async Task Invoke(OutgoingLogicalMessageContext context, Func<OutgoingPhysicalMessageContext, Task> next)
        {
            if (context.GetOrCreate<State>().SkipSerialization)
            {
                await next(new OutgoingPhysicalMessageContext(new byte[0], context.AddressLabels, context)).ConfigureAwait(false);
                return;
            }

            context.SetHeader(Headers.ContentType, messageSerializer.ContentType);
            context.SetHeader(Headers.EnclosedMessageTypes, SerializeEnclosedMessageTypes(context.Message.MessageType));

            var array = Serialize(context);
            await next(new OutgoingPhysicalMessageContext(array, context.AddressLabels, context)).ConfigureAwait(false);
        }

        byte[] Serialize(OutgoingLogicalMessageContext context)
        {
            using (var ms = new MemoryStream())
            {
                messageSerializer.Serialize(context.Message.Instance, ms);
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
        public static void SkipSerialization(this OutgoingLogicalMessageContext context)
        {
            context.GetOrCreate<SerializeMessageConnector.State>().SkipSerialization = true;
        }
    }
}