namespace NServiceBus.Gateway.Config.Channels
{
    using System.Collections.Generic;
    using System.Configuration;
    using Gateway.Channels;
    using Gateway.Channels.Http;

    public class LegacyChannelManager : IManageChannels
    {
        public IEnumerable<Channel> GetActiveChannels()
        {
            var n = ConfigurationManager.AppSettings["NumberOfWorkerThreads"];

            int numberOfWorkerThreads;

            if (!int.TryParse(n, out numberOfWorkerThreads))
                numberOfWorkerThreads = 10;

            var listenUrl = ConfigurationManager.AppSettings["ListenUrl"];

            yield return new Channel
                       {
                           NumWorkerThreads = numberOfWorkerThreads,
                           ReceiveAddress = listenUrl,
                           Receiver = typeof(HttpChannelReceiver)
                       };
        }
    }
}