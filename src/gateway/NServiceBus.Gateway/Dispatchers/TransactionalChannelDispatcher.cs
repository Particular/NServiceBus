namespace NServiceBus.Gateway.Dispatchers
{
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using Channels;
    using Channels.Http;
    using Notifications;
    using Routing;
    using Unicast.Queuing;
    using Unicast.Queuing.Msmq;
    using Unicast.Transport;
    using Unicast.Transport.Transactional;

    public class TransactionalChannelDispatcher : IDispatchMessagesToChannels
    {
        public string InputQueue { get; set; }

        public TransactionalChannelDispatcher(IChannelFactory channelFactory,
                                                IMessageNotifier notifier,
                                                ISendMessages messageSender,
                                                IRouteMessages routeMessages)
        {
            this.routeMessages = routeMessages;
            this.channelFactory = channelFactory;
            this.notifier = notifier;
            this.messageSender = messageSender;
        }

        public void Start()
        {
            var templateTransport = Configure.Instance.Builder.Build<TransactionalTransport>();

            transport = new TransactionalTransport
                            {
                                //todo grab the receiver from the main bus and clone
                                MessageReceiver = new MsmqMessageReceiver(),
                                IsTransactional = true,
                                NumberOfWorkerThreads = templateTransport.NumberOfWorkerThreads == 0 ? 1 : templateTransport.NumberOfWorkerThreads,
                                MaxRetries = templateTransport.MaxRetries,
                                FailureManager = templateTransport.FailureManager
                            };

            transport.TransportMessageReceived += OnTransportMessageReceived;

            transport.Start(InputQueue);
        }

        void OnTransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            var messageToDispatch = e.Message;

            var destinationSites = routeMessages.GetDestinationSitesFor(messageToDispatch);


            //if there is more than 1 destination we break it up into multiple messages
            if (destinationSites.Count() > 1)
            {
                foreach (var destinationSite in destinationSites)
                    CloneAndSendLocal(messageToDispatch, destinationSite);

                return;
            }

            SendToSite(messageToDispatch, destinationSites.First());
        }

        void CloneAndSendLocal(TransportMessage messageToDispatch, Site destinationSite)
        {
            //todo - do we need to clone? check with Jonathan O
            messageToDispatch.Headers[Headers.DestinationSites] = destinationSite.Key;

            messageSender.Send(messageToDispatch, InputQueue);
        }

        void SendToSite(TransportMessage transportMessage, Site targetSite)
        {
            var headers = new NameValueCollection();

            HeaderMapper.Map(transportMessage, headers);

            var channelSender = channelFactory.CreateChannelSender(targetSite.ChannelType);

            channelSender.Send(targetSite.Address, headers, transportMessage.Body);

            notifier.RaiseMessageForwarded(typeof(MsmqMessageReceiver), channelSender.GetType(), transportMessage);

            //todo get audit settings from the audit settings of the host (possibly allowing to override in config)
            //if (!string.IsNullOrEmpty(audit))
            // messageSender.Send(e.Message, audit);
        }

        readonly IChannelFactory channelFactory;
        readonly IMessageNotifier notifier;
        readonly ISendMessages messageSender;
        ITransport transport;
        readonly IRouteMessages routeMessages;

    }
}