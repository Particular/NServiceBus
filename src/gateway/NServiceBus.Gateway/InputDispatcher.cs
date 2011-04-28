namespace NServiceBus.Gateway
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using Config;
    using Channels;
    using Channels.Http;
    using HeaderManagement;
    using log4net;
    using Notifications;
    using ObjectBuilder;
    using Routing;
    using Unicast.Queuing;
    using Unicast.Transport;
    using Unicast.Transport.Transactional;

    public class InputDispatcher:IDisposable
    {

        public InputDispatcher(   IBuilder builder,
                                                 IManageChannels channelManager,
                                                 IMessageNotifier notifier,
                                                 ISendMessages messageSender,
                                                 IMasterNodeSettings settings)
        {
            this.settings = settings;
            this.builder = builder;
            this.channelManager = channelManager;
            this.notifier = notifier;
            this.messageSender = messageSender;
        }

        public void Start(string inputAddress)
        {
            localAddress = inputAddress;
            addressOfAuditStore = settings.AddressOfAuditStore;

            transport = new TransactionalTransport
                            {
                                MessageReceiver = settings.Receiver,
                                IsTransactional = true,
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
            transportMessage.Headers[Headers.OriginatingSite] = GetCurrentSiteKey();
         
            var headers = new NameValueCollection();

            HeaderMapper.Map(transportMessage, headers);

            headers[GatewayHeaders.IsGatewayMessage] = true.ToString();

            
            var channelSender = GetChannelSenderFor(targetSite);
            
            channelSender.Send(targetSite.Address, headers, transportMessage.Body);

            notifier.RaiseMessageForwarded(settings.Receiver.GetType(), channelSender.GetType(), transportMessage);

            if (!string.IsNullOrEmpty(addressOfAuditStore))
                messageSender.Send(transportMessage, addressOfAuditStore);
        }

        IChannelSender GetChannelSenderFor(Site targetSite)
        {
            return builder.Build(targetSite.ChannelType) as IChannelSender;
        }

        string GetCurrentSiteKey()
        {
            //return the address of the default channel and let the convention based router do it's magic
            return channelManager.GetDefaultChannel().ReceiveAddress;
        }

        string addressOfAuditStore;
        readonly IBuilder builder;
        readonly IManageChannels channelManager;
        readonly IMessageNotifier notifier;
        readonly ISendMessages messageSender;
        ITransport transport;
        readonly IMasterNodeSettings settings;
        string localAddress;

        static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");
       
    }
}