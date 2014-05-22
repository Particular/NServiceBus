namespace NServiceBus.Gateway.Receiving
{
    using System;
    using System.Collections.Generic;
    using Features;
    using Logging;
    using Notifications;
    using Routing;
    using Satellites;
    using Settings;
    using Transports;
    using Unicast;

    public class GatewayReceiver : ISatellite
    {
        public GatewayReceiver()
        {
            activeReceivers = new List<IReceiveMessagesFromSites>();
        }

        public ISendMessages MessageSender { get; set; }
        public IManageReceiveChannels ChannelManager { get; set; }
        public IRouteMessagesToEndpoints EndpointRouter { get; set; }
        public Func<IReceiveMessagesFromSites> builder { get; set; }

        public void Stop()
        {
            Logger.InfoFormat("Receiver is shutting down");

            foreach (var channelReceiver in activeReceivers)
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
            get { return !Feature.IsEnabled<Gateway>(); }
        }

        public void Start()
        {
            replyToAddress = SettingsHolder.Instance.Get<Address>("Gateway.InputAddress");

            foreach (var receiveChannel in ChannelManager.GetReceiveChannels())
            {
                var receiver = builder();

                receiver.MessageReceived += MessageReceivedOnChannel;
                receiver.Start(receiveChannel, receiveChannel.NumberOfWorkerThreads);
                activeReceivers.Add(receiver);

                Logger.InfoFormat("Receive channel started: {0}", receiveChannel);
            }
        }

        void MessageReceivedOnChannel(object sender, MessageReceivedOnChannelArgs e)
        {
            var messageToSend = e.Message;

            messageToSend.ReplyToAddress = replyToAddress;

            var destination = EndpointRouter.GetDestinationFor(messageToSend);

            Logger.Info("Sending message to " + destination);

            MessageSender.Send(messageToSend, new SendOptions(destination));
        }

        static ILog Logger = LogManager.GetLogger<GatewayReceiver>();
        readonly ICollection<IReceiveMessagesFromSites> activeReceivers;

        Address replyToAddress;
    }
}