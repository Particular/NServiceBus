namespace NServiceBus.Unicast.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MessageHeaders;
    using MessageInterfaces;
    using Pipeline;

    class LogicalMessageFactory
    {
        public MessageMetadataRegistry MessageMetadataRegistry { get; set; }
        
        public IMessageMapper MessageMapper { get; set; }

        public PipelineFactory PipelineFactory { get; set; }

        public IEnumerable<LogicalMessage> Create<T>(T message)
        {
            return new[] { Create(message.GetType(), message) };
        }

        public LogicalMessage Create(Type messageType, object message)
        {
             var headers = GetMessageHeaders(message);

            return Create(messageType, message, headers);
        }

        public LogicalMessage Create(Type messageType, object message, Dictionary<string, string> headers)
        {
            var realMessageType = MessageMapper.GetMappedTypeFor(messageType);

            return new LogicalMessage(MessageMetadataRegistry.GetMessageDefinition(realMessageType), message, headers);
        }


        //in v5 we can skip this since we'll only support one message and the creation of messages happens under our control so we can capture 
        // the real message type without using the mapper
        [ObsoleteEx(RemoveInVersion = "5.0")]
        public IEnumerable<LogicalMessage> CreateMultiple(IEnumerable<object> messages)
        {
            if (messages == null)
            {
                return new List<LogicalMessage>();
            }


            return messages.Select(m =>
            {
                var messageType = MessageMapper.GetMappedTypeFor(m.GetType());
                var headers = GetMessageHeaders(m);
       
                return new LogicalMessage(MessageMetadataRegistry.GetMessageDefinition(messageType), m,headers);
            }).ToList();
        }


        Dictionary<string, string> GetMessageHeaders(object message)
        {
            Dictionary<object, Dictionary<string, string>> outgoingHeaders;

            if (!PipelineFactory.CurrentContext.TryGet("NServiceBus.OutgoingHeaders", out outgoingHeaders))
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