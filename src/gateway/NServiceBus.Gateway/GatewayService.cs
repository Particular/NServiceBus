namespace NServiceBus.Gateway
{
    using System;
    using System.Collections.Generic;
    using Channels;
    using Channels.Http;
    using Dispatchers;
    using log4net;
    using Notifications;
    using Unicast.Queuing;
    using Unicast.Transport;

    public class GatewayService:IDisposable
    {
        public string DefaultDestinationAddress { get; set; }

        public string InputAddress { get; set; }

        public GatewayService(IDispatchMessagesToChannels channelDispatcher, ISendMessages messageSender)
        {
            this.channelDispatcher = channelDispatcher;
            channels = Configure.Instance.Builder.BuildAll<IChannelReceiver>();
      
            this.messageSender = messageSender;
        }

        public void Start()
        {
            channelDispatcher.Start(InputAddress);

            foreach (var channel in channels)
            {
                channel.MessageReceived += MessageReceivedOnChannel;
                channel.Start();          
            }
        }

        //todo abstract this behind a "output forwarder"
        void MessageReceivedOnChannel(object sender, MessageReceivedOnChannelArgs e)
        {
            var messageToSend = e.Message;

            messageToSend.ReturnAddress = InputAddress;

            string destination = GetDestination(messageToSend);

            Logger.Info("Sending message to " + destination);

            messageSender.Send(messageToSend, destination);
        }

        string GetDestination(TransportMessage messageToSend)
        {
            string routeTo = Headers.RouteTo.Replace(HeaderMapper.NServiceBus + Headers.HeaderName + ".", "");
            var destination = DefaultDestinationAddress;
           
            if (messageToSend.Headers.ContainsKey(routeTo))
                destination = messageToSend.Headers[routeTo];
            return destination;
        }

        public void Dispose()
        {
            foreach (var channel in channels)
            {
                channel.Stop();
            }
        }

        static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Gateway");
        readonly ISendMessages messageSender;
        readonly IEnumerable<IChannelReceiver> channels;
        readonly IDispatchMessagesToChannels channelDispatcher;
    }
}
