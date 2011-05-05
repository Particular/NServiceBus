﻿namespace NServiceBus.Gateway
{
    using System;
    using System.Collections.Generic;
    using Channels;
    using Channels.Http;
    using Dispatchers;
    using log4net;
    using Notifications;
    using Unicast.Queuing;

    public class GatewayService:IDisposable
    {
        public string DefaultDestinationAddress { get; set; }

        public string ReturnAddress { get; set; }

        public GatewayService(IDispatchMessagesToChannels channelDispatcher, ISendMessages messageSender)
        {
            this.channelDispatcher = channelDispatcher;
            channels = Configure.Instance.Builder.BuildAll<IChannelReceiver>();
      
            this.messageSender = messageSender;
        }

        public void Start()
        {
            channelDispatcher.Start();

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

            messageToSend.ReturnAddress = ReturnAddress;

            string routeTo = Headers.RouteTo.Replace(HeaderMapper.NServiceBus + Headers.HeaderName + ".", "");
            var destination = DefaultDestinationAddress;
           
            if (messageToSend.Headers.ContainsKey(routeTo))
                destination = messageToSend.Headers[routeTo];
           
            Logger.Info("Sending message to " + destination);

            messageSender.Send(messageToSend, destination);
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