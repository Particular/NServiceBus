namespace NServiceBus.Gateway
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Channels;
    using Channels.Http;
    using Dispatchers;
    using log4net;
    using Notifications;
    using ObjectBuilder;
    using Unicast.Queuing;
    using Unicast.Transport;

    public class GatewayService : IDisposable
    {
        public string DefaultDestinationAddress { get; set; }

        public string GatewayInputAddress { get; set; }

        public GatewayService(IDispatchMessagesToChannels channelDispatcher,
                                ISendMessages messageSender,
                                IManageChannels channelManager,
                                IBuilder builder)
        {
            this.channelDispatcher = channelDispatcher;

            this.messageSender = messageSender;
            this.channelManager = channelManager;
            this.builder = builder;
            channelReceivers = new List<IChannelReceiver>();
        }

        public void Start()
        {
            channelDispatcher.Start(GatewayInputAddress);

            foreach (var channel in channelManager.GetActiveChannels())
            {

                var channelReceiver = (IChannelReceiver)builder.Build(channel.Receiver);

                channelReceiver.MessageReceived += MessageReceivedOnChannel;
                channelReceiver.Start(channel.ReceiveAddress, channel.NumWorkerThreads);

                channelReceivers.Add(channelReceiver);
            }
        }

        //todo abstract this behind a "output forwarder"
        void MessageReceivedOnChannel(object sender, MessageReceivedOnChannelArgs e)
        {
            var messageToSend = e.Message;

            messageToSend.ReturnAddress = GatewayInputAddress;

            string destination = GetDestination(messageToSend);

            Logger.Info("Sending message to " + destination);

            messageSender.Send(messageToSend, destination);
        }

        string GetDestination(TransportMessage messageToSend)
        {
            //todo - figure out why we use a funny name for this header
            string routeTo = Headers.RouteTo.Replace(HeaderMapper.NServiceBus + Headers.HeaderName + ".", "");
            var destination = DefaultDestinationAddress;

            if (messageToSend.Headers.ContainsKey(routeTo))
                destination = messageToSend.Headers[routeTo];
            return destination;
        }

        public void Dispose()
        {
            foreach (var channel in channelReceivers)
                channel.Stop(); //todo - use dispose instead
        }

        static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");
        readonly ISendMessages messageSender;
        readonly IManageChannels channelManager;
        readonly IBuilder builder;
        readonly ICollection<IChannelReceiver> channelReceivers;
        readonly IDispatchMessagesToChannels channelDispatcher;
    }
}
