namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using Messages;
    using Pipeline;
    using Pipeline.Contexts;
    using Serialization;

    /// <summary>
    /// Not for public consumption. May change in minor version releases.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class SerializeMessagesBehavior : IBehavior<SendPhysicalMessageContext>
    {
        IMessageSerializer messageSerializer;

        internal SerializeMessagesBehavior(IMessageSerializer messageSerializer)
        {
            this.messageSerializer = messageSerializer;
        }

        public void Invoke(SendPhysicalMessageContext context, Action next)
        {
            if (context.LogicalMessages.Any())
            {
                using (var ms = new MemoryStream())
                {
                    var messages = context.LogicalMessages.Select(m => m.Instance).ToArray();

                    messageSerializer.Serialize(messages, ms);

                    context.MessageToSend.Headers[Headers.ContentType] = messageSerializer.ContentType;

                    context.MessageToSend.Headers[Headers.EnclosedMessageTypes] = SerializeEnclosedMessageTypes(context.LogicalMessages);

                    context.MessageToSend.Body = ms.ToArray();
                }
              
                foreach (var headerEntry in context.LogicalMessages.SelectMany(lm => lm.Headers))
                {
                    context.MessageToSend.Headers[headerEntry.Key] = headerEntry.Value;
                }
                
            }

            next();
        }

        string SerializeEnclosedMessageTypes(IEnumerable<LogicalMessage> messages)
        {
            var distinctTypes = messages.SelectMany(lm => lm.Metadata.MessageHierarchy).Distinct();
            
            return string.Join(";", distinctTypes.Select(t => t.AssemblyQualifiedName));
        }
    }
}