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

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class SerializeMessagesBehavior : IBehavior<SendPhysicalMessageContext>
    {
        public IMessageSerializer MessageSerializer { get; set; }

        public void Invoke(SendPhysicalMessageContext context, Action next)
        {
            if (context.LogicalMessage != null)
            {
                using (var ms = new MemoryStream())
                {
                    
                    MessageSerializer.Serialize(new[]{context.LogicalMessage.Instance}, ms);

                    context.MessageToSend.Headers[Headers.ContentType] = MessageSerializer.ContentType;

                    context.MessageToSend.Headers[Headers.EnclosedMessageTypes] = SerializeEnclosedMessageTypes(context.LogicalMessage);

                    context.MessageToSend.Body = ms.ToArray();
                }
              
                foreach (var headerEntry in context.LogicalMessage.Headers)
                {
                    context.MessageToSend.Headers[headerEntry.Key] = headerEntry.Value;
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