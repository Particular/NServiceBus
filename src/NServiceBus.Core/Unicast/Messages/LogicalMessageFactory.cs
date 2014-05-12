namespace NServiceBus.Unicast.Messages
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using MessageInterfaces;
    using Pipeline;


    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class LogicalMessageFactory
    {
        public MessageMetadataRegistry MessageMetadataRegistry { get; set; }
        
        public IMessageMapper MessageMapper { get; set; }

        public PipelineExecutor PipelineExecutor { get; set; }

        public LogicalMessage Create(object message)
        {
            return Create(message.GetType(), message);
        }

        public LogicalMessage Create(Type messageType, object message)
        {
             var headers = GetMessageHeaders(message);

            return Create(messageType, message, headers);
        }

        public LogicalMessage Create(Type messageType, object message, Dictionary<string, string> headers)
        {
            var realMessageType = MessageMapper.GetMappedTypeFor(messageType);

            return new LogicalMessage(MessageMetadataRegistry.GetMessageDefinition(realMessageType), message, headers, this);
        }

        public LogicalMessage CreateControl(Dictionary<string, string> headers)
        {
            headers.Add(Headers.ControlMessageHeader, true.ToString());

            return new LogicalMessage(headers, this);
        }

        Dictionary<string, string> GetMessageHeaders(object message)
        {
            Dictionary<object, Dictionary<string, string>> outgoingHeaders;

            if (!PipelineExecutor.CurrentContext.TryGet("NServiceBus.OutgoingHeaders", out outgoingHeaders))
            {
                return new Dictionary<string, string>();
            }
            Dictionary<string, string> outgoingHeadersForThisMessage;

            if (!outgoingHeaders.TryGetValue(message, out outgoingHeadersForThisMessage))
            {
                return new Dictionary<string, string>();
            }

            //remove the entry to allow memory to be reclaimed
            outgoingHeaders.Remove(message);

            return outgoingHeadersForThisMessage;
        }
    }
}