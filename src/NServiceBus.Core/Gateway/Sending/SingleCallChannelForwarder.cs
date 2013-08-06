namespace NServiceBus.Gateway.Sending
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Channels;
    using Channels.Http;
    using DataBus;
    using HeaderManagement;
    using log4net;
    using Routing;
    using Utils;

    public class SingleCallChannelForwarder : IForwardMessagesToSites
    {
        public SingleCallChannelForwarder(IChannelFactory channelFactory)
        {
            this.channelFactory = channelFactory;
        }

        public IDataBus DataBus { get; set; }

        public void Forward(TransportMessage message, Site targetSite)
        {
            var headers = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

            HeaderMapper.Map(message, headers);

            var channelSender = channelFactory.GetSender(targetSite.Channel.Type);

            //databus properties have to be available at the receiver site
            //before the body of the message is forwarded on the bus
            TransmittDataBusProperties(channelSender, targetSite, headers);

            using (var messagePayload = new MemoryStream(message.Body))
            {
                Transmit(channelSender, targetSite, CallType.SingleCallSubmit, headers, messagePayload);
            }
        }

        void Transmit(IChannelSender channelSender, Site targetSite, CallType callType,
            IDictionary<string, string> headers, Stream data)
        {
            headers[GatewayHeaders.IsGatewayMessage] = Boolean.TrueString;
            headers[HeaderMapper.NServiceBus + HeaderMapper.CallType] = Enum.GetName(typeof(CallType), callType);
            headers[HttpHeaders.ContentMd5Key] = Hasher.Hash(data);

            Logger.DebugFormat("Sending message - {0} to: {1}", callType, targetSite.Channel.Address);

            channelSender.Send(targetSite.Channel.Address, headers, data);
        }

        void TransmittDataBusProperties(IChannelSender channelSender, Site targetSite,
            IDictionary<string, string> headers)
        {
            var headersToSend = new Dictionary<string, string>(headers);

            foreach (
                var headerKey in headers.Keys.Where(headerKey => headerKey.Contains(HeaderMapper.DATABUS_PREFIX)))
            {
                if (DataBus == null)
                {
                    throw new InvalidOperationException(
                        "Can't send a message with a databus property without a databus configured");
                }

                headersToSend[GatewayHeaders.DatabusKey] = headerKey;

                var databusKeyForThisProperty = headers[headerKey];

                using (var stream = DataBus.Get(databusKeyForThisProperty))
                {
                    Transmit(channelSender, targetSite, CallType.SingleCallDatabusProperty, headersToSend, stream);
                }
            }
        }

        static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");
        readonly IChannelFactory channelFactory;
    }
}