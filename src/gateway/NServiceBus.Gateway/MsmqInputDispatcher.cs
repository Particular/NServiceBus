namespace NServiceBus.Gateway
{
    using Unicast.Queuing.Msmq;
    using Unicast.Transport;
    using Unicast.Transport.Transactional;

    public class MsmqInputDispatcher
    {
        public string RemoteAddress { get; set; }

        public string InputQueue { get; set; }

        public MsmqInputDispatcher(IChannel channel, IMessageNotifier notifier)
        {
            this.channel = channel;
            this.notifier = notifier;
        }

        public void Start()
        {
            var templateTransport = Configure.Instance.Builder.Build<TransactionalTransport>();

            transport = new TransactionalTransport
                            {
                                MessageReceiver = new MsmqMessageReceiver(),
                                IsTransactional = true,
                                NumberOfWorkerThreads = templateTransport.NumberOfWorkerThreads == 0 ? 1 : templateTransport.NumberOfWorkerThreads,
                                MaxRetries = templateTransport.MaxRetries,
                                FailureManager = templateTransport.FailureManager
                            };


            transport.TransportMessageReceived += (s, e) =>
            {
                var address = GetRemoteAddress(RemoteAddress, e.Message);

                channel.Send(e.Message, address);

                notifier.RaiseMessageForwarded(ChannelType.Msmq, ChannelType.Http, e.Message);

                //todo get audit settings from the audit settings of the host (possibly allowing to override in config)
                //if (!string.IsNullOrEmpty(audit))
                // messageSender.Send(e.Message, audit);
            };
            transport.Start(InputQueue);
        }

        static string GetRemoteAddress(string remoteUrl, TransportMessage msg)
        {
            var address = remoteUrl;


            //todo use a more generic header name, SiteKey or SiteName perhaps
            if (msg.Headers.ContainsKey(Headers.HttpTo))
                address = msg.Headers[Headers.HttpTo];
            return address;
        }

        readonly IChannel channel;
        readonly IMessageNotifier notifier;
        ITransport transport;

    }
}