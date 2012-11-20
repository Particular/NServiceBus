namespace NServiceBus.Gateway.Sending
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Logging;
    using Notifications;
    using ObjectBuilder;
    using Receiving;
    using Routing;
    using Unicast.Queuing;
    using Unicast.Transport;
    using Unicast;

    public class GatewaySender:IDisposable
    {
        public ITransport Transport { get; set; }
        public UnicastBus UnicastBus { get; set; }

        public GatewaySender(   IBuilder builder,
                                                 IMangageReceiveChannels channelManager,
                                                 IMessageNotifier notifier,
                                                 ISendMessages messageSender)
        {
            this.builder = builder;
            this.channelManager = channelManager;
            this.notifier = notifier;
            this.messageSender = messageSender;
        }



        public void Start(Address inputAddress)
        {
            localAddress = inputAddress;
       
            Transport.TransportMessageReceived += OnTransportMessageReceived;

            Transport.Start(localAddress);

            Logger.InfoFormat("Gateway started listening on inputs on - {0}", localAddress);
        }

        public void Dispose()
        {
            Transport.Dispose();
            Logger.InfoFormat("Gateway stopped");
        }
        void OnTransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            var messageToDispatch = e.Message;

            var destinationSites = GetDestinationSitesFor(messageToDispatch);

            //if there is more than 1 destination we break it up into multiple messages
            if (destinationSites.Count() > 1)
            {
                foreach (var destinationSite in destinationSites)
                    CloneAndSendLocal(messageToDispatch, destinationSite);

                return;
            }

            var destination = destinationSites.FirstOrDefault();

            if (destination == null)
                throw new InvalidOperationException("No destination found for message");


            SendToSite(messageToDispatch, destination);
        }

        IEnumerable<Site> GetDestinationSitesFor(TransportMessage messageToDispatch)
        {
            return builder.BuildAll<IRouteMessagesToSites>()
                .SelectMany(r => r.GetDestinationSitesFor(messageToDispatch));
        }

        void CloneAndSendLocal(TransportMessage messageToDispatch, Site destinationSite)
        {
            //todo - do we need to clone? check with Jonathan O
            messageToDispatch.Headers[Headers.DestinationSites] = destinationSite.Key;

            messageSender.Send(messageToDispatch, localAddress);
        }

        void SendToSite(TransportMessage transportMessage, Site targetSite)
        {
            transportMessage.Headers[Headers.OriginatingSite] = GetDefaultAddressForThisSite();


            //todo - derive this from the message and the channeltype
            builder.Build<IdempotentChannelForwarder>()
                .Forward(transportMessage,targetSite);

            notifier.RaiseMessageForwarded("msmq", targetSite.Channel.Type, transportMessage);

            if (UnicastBus != null && UnicastBus.ForwardReceivedMessagesTo != null && UnicastBus.ForwardReceivedMessagesTo != Address.Undefined)
                messageSender.Send(transportMessage, UnicastBus.ForwardReceivedMessagesTo);
        }

       
        string GetDefaultAddressForThisSite()
        {
            return channelManager.GetDefaultChannel().ToString();
        }

        readonly IBuilder builder;
        readonly IMangageReceiveChannels channelManager;
        readonly IMessageNotifier notifier;
        readonly ISendMessages messageSender;
        Address localAddress;

        static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");
    }
}
