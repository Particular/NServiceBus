namespace NServiceBus.Unicast.Messages
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using MessageInterfaces;
    using Pipeline;


    /// <summary>
    /// Not for public consumption. May change in minor version releases.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class LogicalMessageFactory
    {
        PipelineFactory pipelineFactory;
        IMessageMapper messageMapper;
        MessageMetadataRegistry messageMetadataRegistry;

        public LogicalMessageFactory(PipelineFactory pipelineFactory, IMessageMapper messageMapper, MessageMetadataRegistry messageMetadataRegistry)
        {
            this.pipelineFactory = pipelineFactory;
            this.messageMapper = messageMapper;
            this.messageMetadataRegistry = messageMetadataRegistry;
        }

        public List<LogicalMessage> Create<T>(T message)
        {
            return new[] { Create(message.GetType(), message) }.ToList();
        }

        internal LogicalMessage Create(Type messageType, object message)
        {
             var headers = GetMessageHeaders(message);

            return Create(messageType, message, headers);
        }

        internal LogicalMessage Create(Type messageType, object message, Dictionary<string, string> headers)
        {
            var realMessageType = messageMapper.GetMappedTypeFor(messageType);

            return new LogicalMessage(messageMetadataRegistry.GetMessageDefinition(realMessageType), message, headers);
        }


        //in v5 we can skip this since we'll only support one message and the creation of messages happens under our control so we can capture 
        // the real message type without using the mapper
        [ObsoleteEx(RemoveInVersion = "5.0")]
        internal List<LogicalMessage> CreateMultiple(IEnumerable<object> messages)
        {
            if (messages == null)
            {
                return new List<LogicalMessage>();
            }


            return messages.Select(m =>
            {
                var messageType = messageMapper.GetMappedTypeFor(m.GetType());
                var headers = GetMessageHeaders(m);
       
                return new LogicalMessage(messageMetadataRegistry.GetMessageDefinition(messageType), m,headers);
            }).ToList();
        }


        Dictionary<string, string> GetMessageHeaders(object message)
        {
            Dictionary<object, Dictionary<string, string>> outgoingHeaders;

            if (!pipelineFactory.CurrentContext.TryGet("NServiceBus.OutgoingHeaders", out outgoingHeaders))
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