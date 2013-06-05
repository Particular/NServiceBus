namespace NServiceBus.Gateway.Sending
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Config;
    using Features;
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

        public bool Handle(TransportMessage message)
        {
            IList<Site> destinationSites = GetDestinationSitesFor(message);

            //if there is more than 1 destination we break it up into multiple messages
            if (destinationSites.Count() > 1)
            {
                foreach (Site destinationSite in destinationSites)
                {
                    CloneAndSendLocal(message, destinationSite);
                }

                return true;
            }

            Site destination = destinationSites.FirstOrDefault();

            if (destination == null)
                throw new InvalidOperationException("No destination found for message");

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

        private IList<Site> GetDestinationSitesFor(TransportMessage messageToDispatch)
        {
            return Builder.BuildAll<IRouteMessagesToSites>()
                          .SelectMany(r => r.GetDestinationSitesFor(messageToDispatch)).ToList();
        }

        private void CloneAndSendLocal(TransportMessage messageToDispatch, Site destinationSite)
        {
            //todo - do we need to clone? check with Jonathan O
            messageToDispatch.Headers[Headers.DestinationSites] = destinationSite.Key;

            MessageSender.Send(messageToDispatch, InputAddress);
        }

        private void SendToSite(TransportMessage transportMessage, Site targetSite)
        {
            transportMessage.Headers[Headers.OriginatingSite] = GetDefaultAddressForThisSite();

            //todo - derive this from the message and the channeltype
            Builder.Build<IdempotentChannelForwarder>()
                   .Forward(transportMessage, targetSite);

            Notifier.RaiseMessageForwarded(Address.Local.ToString(), targetSite.Channel.Type, transportMessage);

            if (UnicastBus != null && UnicastBus.ForwardReceivedMessagesTo != null &&
                UnicastBus.ForwardReceivedMessagesTo != Address.Undefined)
                MessageSender.Send(transportMessage, UnicastBus.ForwardReceivedMessagesTo);
        }


        private string GetDefaultAddressForThisSite()
        {
            return ChannelManager.GetDefaultChannel().ToString();
        }

        public Action<TransportReceiver> GetReceiverCustomization()
        {
            return transport =>
            {
                var configSection = Configure.ConfigurationSource.GetConfiguration<GatewayConfig>();
                if (configSection.TransactionTimeout > transport.TransactionSettings.TransactionTimeout)
                    transport.TransactionSettings.TransactionTimeout = configSection.TransactionTimeout;
            };
        }
    }
}