namespace NServiceBus.Gateway.Receiving
{
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using Channels;
    using NServiceBus.Gateway.Channels.Http;

    public class LegacyChannelManager : IMangageReceiveChannels
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

        public Channel GetDefaultChannel()
        {
            return GetActiveChannels().First();
        }
    }
}