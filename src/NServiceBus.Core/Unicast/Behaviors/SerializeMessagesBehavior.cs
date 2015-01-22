namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Linq;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Messages;
    using NServiceBus.Unicast.Transport;
    using Serialization;

    class SerializeMessagesBehavior : PhysicalOutgoingContextStageBehavior
    {
        readonly IMessageSerializer messageSerializer;

        public SerializeMessagesBehavior(IMessageSerializer messageSerializer)
        {
            this.messageSerializer = messageSerializer;
        }

        public override void Invoke(Context context, Action next)
        {
            if (!context.OutgoingMessage.IsControlMessage())
            {
                using (var ms = new MemoryStream())
                {
                    
                    messageSerializer.Serialize(context.OutgoingLogicalMessage.Instance, ms);

                    context.OutgoingMessage.Headers[Headers.ContentType] = messageSerializer.ContentType;

                    context.OutgoingMessage.Headers[Headers.EnclosedMessageTypes] = SerializeEnclosedMessageTypes(context.OutgoingLogicalMessage);

                    context.OutgoingMessage.Body = ms.ToArray();
                }

                foreach (var headerEntry in context.OutgoingLogicalMessage.Headers)
                {
                    context.OutgoingMessage.Headers[headerEntry.Key] = headerEntry.Value;
                }
            }

            next();
        }

        string SerializeEnclosedMessageTypes(LogicalMessage message)
        {
            var distinctTypes = message.Metadata.MessageHierarchy.Distinct();
            
            return string.Join(";", distinctTypes.Select(t => t.AssemblyQualifiedName));
        }

    }
}
