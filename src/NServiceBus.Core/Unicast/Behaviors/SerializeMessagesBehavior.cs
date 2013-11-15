namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Messages;
    using Pipeline;
    using Pipeline.Contexts;
    using Serialization;

    class SerializeMessagesBehavior : IBehavior<SendPhysicalMessageContext>
    {
        public IMessageSerializer MessageSerializer { get; set; }

        public void Invoke(SendPhysicalMessageContext context, Action next)
        {
            if (context.LogicalMessages.Any())
            {
                using (var ms = new MemoryStream())
                {
                    var messages = context.LogicalMessages.Select(m => m.Instance).ToArray();

                    MessageSerializer.Serialize(messages, ms);

                    context.MessageToSend.Headers[Headers.ContentType] = MessageSerializer.ContentType;

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