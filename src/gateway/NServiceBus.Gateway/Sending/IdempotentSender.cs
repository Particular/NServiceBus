namespace NServiceBus.Gateway.Sending
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using Channels;
    using Channels.Http;
    using DataBus;
    using HeaderManagement;
    using log4net;
    using ObjectBuilder;
    using Routing;
    using Unicast.Transport;
    using Utils;

    public class IdempotentSender:ISendMessagesToSites
    {
        readonly IBuilder builder;

        public IdempotentSender(IBuilder builder)
        {
            this.builder = builder;
        }

        public void Send(Site targetSite, TransportMessage message)
        {
            var headers = new Dictionary<string,string>();

            HeaderMapper.Map(message, headers);
            
            var channelSender = GetChannelSenderFor(targetSite);

            using(var messagePayload = new MemoryStream(message.Body))
                Transmit(channelSender,targetSite, CallType.Submit, headers, messagePayload);

            TransmittDataBusProperties(channelSender,targetSite, headers);

            Transmit(channelSender,targetSite, CallType.Ack, headers, new MemoryStream());
        }

         
        void Transmit(IChannelSender channelSender, Site targetSite, CallType callType, IDictionary<string,string> headers, Stream data)
        {
            headers[HeaderMapper.NServiceBus + HeaderMapper.CallType] = Enum.GetName(typeof(CallType), callType);
            headers[HttpHeaders.ContentMd5Key] = Hasher.Hash(data);

            Logger.DebugFormat("Sending message - {0} to: {1}", callType, targetSite.Address);
            
            channelSender.Send(targetSite.Address, headers, data);
        }

     
        void TransmittDataBusProperties(IChannelSender channelSender,Site targetSite, IDictionary<string,string> headers)
        {
            var headersToSend = new Dictionary<string,string> (headers);


            foreach (string headerKey in headers.Keys)
            {
                if (headerKey.Contains(DATABUS_PREFIX))
                {
                    if (DataBus == null)
                        throw new InvalidOperationException("Can't send a message with a databus property without a databus configured");

                    headersToSend[GatewayHeaders.DatabusKey] = headerKey;

                    using (var stream = DataBus.Get(headers[headerKey]))
                        Transmit(channelSender,targetSite, CallType.DatabusProperty, headersToSend, stream);
                }

            }

        }

        IChannelSender GetChannelSenderFor(Site targetSite)
        {
            return builder.Build(targetSite.ChannelType) as IChannelSender;
        }

        public IDataBus DataBus { get; set; }

        const string DATABUS_PREFIX = "NServiceBus.DataBus.";

        static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");
 
    }
}