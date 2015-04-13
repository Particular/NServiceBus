namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Linq;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Serialization;
    using NServiceBus.Unicast.Messages;

    class SerializeMessagesBehavior : StageConnector<OutgoingContext, PhysicalOutgoingContextStageBehavior.Context>
    {

        readonly IMessageSerializer messageSerializer;
        readonly MessageMetadataRegistry messageMetadataRegistry;

        public SerializeMessagesBehavior(IMessageSerializer messageSerializer,MessageMetadataRegistry messageMetadataRegistry)
        {
            this.messageSerializer = messageSerializer;
            this.messageMetadataRegistry = messageMetadataRegistry;
        }

        public override void Invoke(OutgoingContext context, Action<PhysicalOutgoingContextStageBehavior.Context> next)
        {
            if (context.IsControlMessage)
            {
                next(new PhysicalOutgoingContextStageBehavior.Context(new byte[0], context));
                return;
            }

            using (var ms = new MemoryStream())
            {

                messageSerializer.Serialize(context.MessageInstance, ms);

                context.Headers[Headers.ContentType] = messageSerializer.ContentType;

                context.Headers[Headers.EnclosedMessageTypes] = SerializeEnclosedMessageTypes(context.MessageType);
                next(new PhysicalOutgoingContextStageBehavior.Context(ms.ToArray(), context));
            }
        }

        string SerializeEnclosedMessageTypes(Type messageType)
        {
            var metadata = messageMetadataRegistry.GetMessageMetadata(messageType);
            var distinctTypes = metadata.MessageHierarchy.Distinct();

            return string.Join(";", distinctTypes.Select(t => t.AssemblyQualifiedName));
        }

    }
}