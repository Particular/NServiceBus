﻿namespace NServiceBus.Gateway.Sending
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Channels;
    using Channels.Http;
    using DataBus;
    using HeaderManagement;
    using Unicast.Transport;
    using log4net;
    using Routing;
    using Utils;

    public class IdempotentChannelForwarder : IForwardMessagesToSites
    {
        private readonly IChannelFactory channelFactory;

        public IdempotentChannelForwarder(IChannelFactory channelFactory)
        {
            this.channelFactory = channelFactory;
        }

        public void Forward(TransportMessage message, Site targetSite)
        {
            var headers = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

            HeaderMapper.Map(message, headers);

            var channelSender = channelFactory.GetSender(targetSite.Channel.Type);

            using (var messagePayload = new MemoryStream(message.Body))
                Transmit(channelSender, targetSite, CallType.Submit, headers, messagePayload);

            TransmittDataBusProperties(channelSender, targetSite, headers);

            Transmit(channelSender, targetSite, CallType.Ack, headers, new MemoryStream());
        }

        private void Transmit(IChannelSender channelSender, Site targetSite, CallType callType,
                              IDictionary<string, string> headers, Stream data)
        {
            headers[GatewayHeaders.IsGatewayMessage] = Boolean.TrueString;
            headers[HeaderMapper.NServiceBus + HeaderMapper.CallType] = Enum.GetName(typeof (CallType), callType);
            headers[HttpHeaders.ContentMd5Key] = Hasher.Hash(data);

            Logger.DebugFormat("Sending message - {0} to: {1}", callType, targetSite.Channel.Address);

            channelSender.Send(targetSite.Channel.Address, headers, data);
        }

        private void TransmittDataBusProperties(IChannelSender channelSender, Site targetSite,
                                                IDictionary<string, string> headers)
        {
            var headersToSend = new Dictionary<string, string>(headers);

            foreach (string headerKey in headers.Keys.Where(headerKey => headerKey.Contains(DATABUS_PREFIX)))
            {
                if (DataBus == null)
                    throw new InvalidOperationException(
                        "Can't send a message with a databus property without a databus configured");

                headersToSend[GatewayHeaders.DatabusKey] = headerKey;

                var databusKeyForThisProperty = headers[headerKey];

                using (var stream = DataBus.Get(databusKeyForThisProperty))
                    Transmit(channelSender, targetSite, CallType.DatabusProperty, headersToSend, stream);
            }
        }


        public IDataBus DataBus { get; set; }

        private const string DATABUS_PREFIX = "NServiceBus.DataBus.";

        private static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");
    }
}
