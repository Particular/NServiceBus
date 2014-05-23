namespace NServiceBus.Gateway.Sending
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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

    class GatewaySender : IAdvancedSatellite
    {
        public IBuilder Builder { get; set; }
        public IManageReceiveChannels ChannelManager { get; set; }
        public IMessageNotifier Notifier { get; set; }
        public ISendMessages MessageSender { get; set; }

        public GatewayTransaction GatewayTransaction { get; set; }
       
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
            get { return SettingsHolder.Instance.Get<Address>("Gateway.InputAddress"); }
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
                transport.TransactionSettings.TransactionTimeout =
                    GatewayTransaction.Timeout(transport.TransactionSettings.TransactionTimeout);
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

            MessageSender.Send(messageToDispatch, new SendOptions(InputAddress));
        }

        void SendToSite(TransportMessage transportMessage, Site targetSite)
        {
            transportMessage.Headers[Headers.OriginatingSite] = GetDefaultAddressForThisSite();

            var forwarder = Builder.Build<IForwardMessagesToSites>();
            
            forwarder.Forward(transportMessage, targetSite);

            Notifier.RaiseMessageForwarded(Address.Local.ToString(), targetSite.Channel.Type, transportMessage);
        }

        string GetDefaultAddressForThisSite()
        {
            var defaultChannel = ChannelManager.GetDefaultChannel();
            return string.Format("{0},{1}", defaultChannel.Type, defaultChannel.Address);
        }
    }
}
