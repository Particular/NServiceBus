using NServiceBus.Logging;

namespace NServiceBus.Gateway.Sending
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Gateway.Config;
    using NServiceBus.Gateway.Channels;
    using NServiceBus.Gateway.Notifications;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Gateway.Routing;
    using NServiceBus.Unicast.Queuing;
    using NServiceBus.Unicast.Transport;
    using NServiceBus.Unicast.Transport.Transactional;
    using Receiving;

    public class GatewaySender:IDisposable
    {

        public GatewaySender(   IBuilder builder,
                                                 IMangageReceiveChannels channelManager,
                                                 IMessageNotifier notifier,
                                                 ISendMessages messageSender,
                                                 IMainEndpointSettings settings)
        {
            this.settings = settings;
            this.builder = builder;
            this.channelManager = channelManager;
            this.notifier = notifier;
            this.messageSender = messageSender;
        }

        public void Start(Address inputAddress)
        {
            localAddress = inputAddress;
            addressOfAuditStore = settings.AddressOfAuditStore;

            transport = new TransactionalTransport
                            {
                                MessageReceiver = settings.Receiver,
                                IsTransactional = !ConfigureVolatileQueues.IsVolatileQueues,
                                NumberOfWorkerThreads = settings.NumberOfWorkerThreads,
                                MaxRetries = settings.MaxRetries,
                                FailureManager = settings.FailureManager
                            };

            transport.TransportMessageReceived += OnTransportMessageReceived;

            transport.Start(localAddress);

            Logger.InfoFormat("Gateway started listening on inputs on - {0}", localAddress);
        }

        public void Dispose()
        {
            transport.Dispose();
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

            if (addressOfAuditStore != null && addressOfAuditStore != Address.Undefined)
                messageSender.Send(transportMessage, addressOfAuditStore);
        }

       
        string GetDefaultAddressForThisSite()
        {
            return channelManager.GetDefaultChannel().ToString();
        }

        Address addressOfAuditStore;
        readonly IBuilder builder;
        readonly IMangageReceiveChannels channelManager;
        readonly IMessageNotifier notifier;
        readonly ISendMessages messageSender;
        ITransport transport;
        readonly IMainEndpointSettings settings;
        Address localAddress;

        static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");
    }
}