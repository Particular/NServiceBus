namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Pipeline;
    using Pipeline.Contexts;
    using Serialization;

    internal class SerializeMessagesBehavior : IBehavior<SendPhysicalMessageContext>
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
            }

            next();
        }

        string SerializeEnclosedMessageTypes(IEnumerable<LogicalMessage> messages)
        {
            var types = messages.Select(m => m.MessageType).ToList();

            var interfaces = types.SelectMany(t => t.GetInterfaces())
                .Where(MessageConventionExtensions.IsMessageType);

            var distinctTypes = types.Distinct();
            var interfacesOrderedByHierarchy = interfaces.Distinct().OrderByDescending(i => i.GetInterfaces().Count()); // Interfaces with less interfaces are lower in the hierarchy. 

            return string.Join(";", distinctTypes.Concat(interfacesOrderedByHierarchy).Select(t => t.AssemblyQualifiedName));
        }
    }
}