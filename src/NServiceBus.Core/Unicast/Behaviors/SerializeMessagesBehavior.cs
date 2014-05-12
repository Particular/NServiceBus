namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using Messages;
    using Pipeline;
    using Pipeline.Contexts;
    using Serialization;
    using Transport;

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class SerializeMessagesBehavior : IBehavior<SendLogicalMessageContext>
    {
        public IMessageSerializer MessageSerializer { get; set; }

        public void Invoke(SendLogicalMessageContext context, Action next)
        {
            if (context.LogicalMessage.IsControlMessage())
            {
                next();
                return;
            }

            using (var ms = new MemoryStream())
            {

                MessageSerializer.Serialize(new[]
                {
                    context.LogicalMessage.Instance
                }, ms);

                context.OutgoingMessage.Headers[Headers.ContentType] = MessageSerializer.ContentType;

                context.OutgoingMessage.Headers[Headers.EnclosedMessageTypes] = SerializeEnclosedMessageTypes(context.LogicalMessage);

                context.OutgoingMessage.Body = ms.ToArray();
            }

            foreach (var headerEntry in context.LogicalMessage.Headers)
            {
                context.OutgoingMessage.Headers[headerEntry.Key] = headerEntry.Value;
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