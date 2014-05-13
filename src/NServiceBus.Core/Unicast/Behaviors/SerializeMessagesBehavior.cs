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
    public class SerializeMessagesBehavior : IBehavior<OutgoingContext>
    {
        public IMessageSerializer MessageSerializer { get; set; }

        public void Invoke(OutgoingContext context, Action next)
        {
            if (context.OutgoingLogicalMessage.IsControlMessage())
            {
                next();
                return;
            }

            using (var ms = new MemoryStream())
            {

                MessageSerializer.Serialize(new[]
                {
                    context.OutgoingLogicalMessage.Instance
                }, ms);

                context.OutgoingMessage.Headers[Headers.ContentType] = MessageSerializer.ContentType;

                context.OutgoingMessage.Headers[Headers.EnclosedMessageTypes] = SerializeEnclosedMessageTypes(context.OutgoingLogicalMessage);

                context.OutgoingMessage.Body = ms.ToArray();
            }

            foreach (var headerEntry in context.OutgoingLogicalMessage.Headers)
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