namespace NServiceBus.Gateway.Receiving
{
    using System.Collections.Generic;
    using Channels;
    using Logging;
    using Notifications;
    using ObjectBuilder;
    using Routing;
    using Satellites;
    using Transports;
    using Unicast.Queuing;

    public class GatewayReceiver : ISatellite
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(GatewayReceiver));
        private readonly ICollection<IReceiveMessagesFromSites> activeReceivers;
        private readonly Address returnAddress = ConfigureGateway.GatewayInputAddress;

        public GatewayReceiver()
        {
            activeReceivers = new List<IReceiveMessagesFromSites>();
        }

        public ISendMessages MessageSender { get; set; }
        public IManageReceiveChannels ChannelManager { get; set; }
        public IRouteMessagesToEndpoints EndpointRouter { get; set; }
        public IBuilder builder { get; set; }

        public void Stop()
        {
            Logger.InfoFormat("Receiver is shutting down");

            foreach (IReceiveMessagesFromSites channelReceiver in activeReceivers)
            {
                Logger.InfoFormat("Stopping channel - {0}", channelReceiver.GetType());

                channelReceiver.MessageReceived -= MessageReceivedOnChannel;

                channelReceiver.Dispose();
            }

            activeReceivers.Clear();

            Logger.InfoFormat("Receiver shutdown complete");
        }

        public bool Handle(TransportMessage message)
        {
            return true;
        }

        public Address InputAddress
        {
            get { return null; }
        }

        public bool Disabled
        {
            get
            {
                return returnAddress == null;
            }
        }

        public void Start()
        {
            foreach (ReceiveChannel receiveChannel in ChannelManager.GetReceiveChannels())
            {
                var receiver = builder.Build<IReceiveMessagesFromSites>();

                receiver.MessageReceived += MessageReceivedOnChannel;
                receiver.Start(receiveChannel, receiveChannel.NumberOfWorkerThreads);
                activeReceivers.Add(receiver);

                Logger.InfoFormat("Receive channel started: {0}", receiveChannel);
            }
        }

        private void MessageReceivedOnChannel(object sender, MessageReceivedOnChannelArgs e)
        {
            TransportMessage messageToSend = e.Message;

            messageToSend.ReplyToAddress = returnAddress;

            Address destination = EndpointRouter.GetDestinationFor(messageToSend);

            Logger.Info("Sending message to " + destination);

            MessageSender.Send(messageToSend, destination);
        }
    }
}