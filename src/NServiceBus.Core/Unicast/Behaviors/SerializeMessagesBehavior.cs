namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.IO;
    using System.Linq;
    using Messages;
    using Pipeline;
    using Pipeline.Contexts;
    using Serialization;
    using Transport;

    class SerializeMessagesBehavior : IBehavior<OutgoingContext>
    {
        public IMessageSerializer MessageSerializer { get; set; }

        public void Invoke(OutgoingContext context, Action next)
        {
            if (!context.OutgoingMessage.IsControlMessage())
            {
                using (var ms = new MemoryStream())
                {
                    
                    MessageSerializer.Serialize(context.OutgoingLogicalMessage.Instance, ms);

                    context.OutgoingMessage.Headers[Headers.ContentType] = MessageSerializer.ContentType;

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
