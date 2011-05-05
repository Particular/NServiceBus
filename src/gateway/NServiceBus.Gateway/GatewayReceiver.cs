namespace NServiceBus.Gateway
{
    using System;
    using System.Collections.Generic;
    using Channels;
    using log4net;
    using Notifications;
    using ObjectBuilder;
    using Routing;
    using Unicast.Queuing;

    public class GatewayReceiver : IDisposable
    {
        public GatewayReceiver(  IManageChannels channelManager,
                                IRouteMessagesToEndpoints endpointRouter,
                                IBuilder builder, 
                                ISendMessages messageSender)

        {
            this.messageSender = messageSender;
            this.channelManager = channelManager;
            this.endpointRouter = endpointRouter;
            this.builder = builder;
     
            channelReceivers = new List<IChannelReceiver>();
        }

        public void Start(string localAddress)
        {
            returnAddress = localAddress;

            
            foreach (var channel in channelManager.GetActiveChannels())
            {

                var channelReceiver = (IChannelReceiver)builder.Build(channel.Receiver);

                channelReceiver.MessageReceived +=  MessageReceivedOnChannel;
                channelReceiver.Start(channel.ReceiveAddress, channel.NumWorkerThreads);
                channelReceivers.Add(channelReceiver);

                Logger.InfoFormat("Receive channel {0} started. Adress: {1}", channel.Receiver,channel.ReceiveAddress);
            }
        }

        public void Dispose()
        {
            Logger.InfoFormat("Receiver is shutting down");
            
            foreach (var channelReceiver in channelReceivers)
            {
                Logger.InfoFormat("Stopping channel - {0}",channelReceiver.GetType());

                channelReceiver.MessageReceived -= MessageReceivedOnChannel;

                channelReceiver.Dispose();
            }

            channelReceivers.Clear();

            Logger.InfoFormat("Receiver shutdown complete");

        }

        public void MessageReceivedOnChannel(object sender, MessageReceivedOnChannelArgs e)
        {
            var messageToSend = e.Message;

            messageToSend.ReturnAddress = returnAddress;
            
            var destination = endpointRouter.GetDestinationFor(messageToSend);

            Logger.Info("Sending message to " + destination);

            messageSender.Send(messageToSend, destination);
        }

        
        readonly ISendMessages messageSender;
        readonly IManageChannels channelManager;
        readonly IRouteMessagesToEndpoints endpointRouter;
        readonly IBuilder builder;
        readonly ICollection<IChannelReceiver> channelReceivers;
        string returnAddress;
        
        static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");
    }
}
