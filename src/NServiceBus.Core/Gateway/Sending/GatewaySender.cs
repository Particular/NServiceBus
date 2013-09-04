namespace NServiceBus.Gateway.Sending
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Config;
    using Audit;
    using Features;
    using HeaderManagement;
    using Notifications;
    using ObjectBuilder;
    using Receiving;
    using Routing;
    using Satellites;
    using Settings;
    using Transports;
    using Unicast;
    using Unicast.Transport;

    public class GatewaySender : IAdvancedSatellite
    {
        public UnicastBus UnicastBus { get; set; }
        public IBuilder Builder { get; set; }
        public IManageReceiveChannels ChannelManager { get; set; }
        public IMessageNotifier Notifier { get; set; }
        public ISendMessages MessageSender { get; set; }
        public MessageAuditer MessageAuditer { get; set; }

        public bool Handle(TransportMessage message)
        {
            var destinationSites = GetDestinationSitesFor(message);

            //if there is more than 1 destination we break it up into multiple messages
            if (destinationSites.Count() > 1)
            {
                foreach (var destinationSite in destinationSites)
                {
                    CloneAndSendLocal(message, destinationSite);
                }

                return true;
            }

            var destination = destinationSites.FirstOrDefault();

            if (destination == null)
            {
                throw new InvalidOperationException("No destination found for message");
            }

            SendToSite(message, destination);

            return true;
        }

        public Address InputAddress
        {
            get { return SettingsHolder.Get<Address>("Gateway.InputAddress"); }
        }

        public bool Disabled
        {
            get { return !Feature.IsEnabled<Gateway>(); }
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public Action<TransportReceiver> GetReceiverCustomization()
        {
            return transport =>
            {
                var configSection = Configure.ConfigurationSource.GetConfiguration<GatewayConfig>();
                if (configSection.TransactionTimeout > transport.TransactionSettings.TransactionTimeout)
                {
                    transport.TransactionSettings.TransactionTimeout = configSection.TransactionTimeout;
                }
            };
        }

        IList<Site> GetDestinationSitesFor(TransportMessage messageToDispatch)
        {
            return Builder.BuildAll<IRouteMessagesToSites>()
                .SelectMany(r => r.GetDestinationSitesFor(messageToDispatch)).ToList();
        }

        void CloneAndSendLocal(TransportMessage messageToDispatch, Site destinationSite)
        {
            //todo - do we need to clone? check with Jonathan O
            messageToDispatch.Headers[Headers.DestinationSites] = destinationSite.Key;

            MessageSender.Send(messageToDispatch, InputAddress);
        }

        void SendToSite(TransportMessage transportMessage, Site targetSite)
        {
            transportMessage.Headers[Headers.OriginatingSite] = GetDefaultAddressForThisSite();

            //todo - derive this from the message and the channeltype
            var forwarder = HandleLegacy(transportMessage, targetSite) ??
                            Builder.Build<IForwardMessagesToSites>();
            forwarder.Forward(transportMessage, targetSite);

            Notifier.RaiseMessageForwarded(Address.Local.ToString(), targetSite.Channel.Type, transportMessage);

            // Will forward the message to the configured audit queue if the auditing feature is enabled.
            MessageAuditer.ForwardMessageToAuditQueue(transportMessage);

        }

        IForwardMessagesToSites HandleLegacy(TransportMessage transportMessage, Site targetSite)
        {
            // send to be backwards compatible with Gateway 3.X
            transportMessage.Headers[GatewayHeaders.LegacyMode] = targetSite.LegacyMode.ToString();

            return targetSite.LegacyMode ? Builder.Build<IdempotentChannelForwarder>() : null;
        }

        string GetDefaultAddressForThisSite()
        {
            return ChannelManager.GetDefaultChannel().ToString();
        }
    }
}
