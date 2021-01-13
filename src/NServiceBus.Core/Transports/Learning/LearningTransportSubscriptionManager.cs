using System.Threading;
using NServiceBus.Unicast.Messages;

namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Extensibility;
    using Transport;

    class LearningTransportSubscriptionManager : ISubscriptionManager
    {
        public LearningTransportSubscriptionManager(string basePath, string endpointName, string localAddress)
        {
            this.endpointName = endpointName;
            this.localAddress = localAddress;
            this.basePath = Path.Combine(basePath, ".events");
        }

        public async Task Subscribe(MessageMetadata eventType, ContextBag context)
        {
            var eventDir = GetEventDirectory(eventType.MessageType);

            // the subscription directory and the subscription information will be created no matter if there's a publisher for the event assuming that the publisher haven’t started yet
            Directory.CreateDirectory(eventDir);

            var subscriptionEntryPath = GetSubscriptionEntryPath(eventDir);

            var attempts = 0;

            // since we have a design that can run into concurrency exceptions we perform a few retries
            while (true)
            {
                try
                {
                    await AsyncFile.WriteText(subscriptionEntryPath, localAddress).ConfigureAwait(false);

                    return;
                }
                catch (IOException)
                {
                    attempts++;

                    if (attempts > 10)
                    {
                        throw;
                    }

                    //allow the other task to complete
                    await Task.Delay(100).ConfigureAwait(false);
                }
            }
        }

        public async Task Unsubscribe(MessageMetadata eventType, ContextBag context)
        {
            var eventDir = GetEventDirectory(eventType.MessageType);
            var subscriptionEntryPath = GetSubscriptionEntryPath(eventDir);

            var attempts = 0;

            // since we have a design that can run into concurrency exceptions we perform a few retries
            while (true)
            {
                try
                {
                    if (!File.Exists(subscriptionEntryPath))
                    {
                        return;
                    }

                    File.Delete(subscriptionEntryPath);

                    return;
                }
                catch (IOException)
                {
                    attempts++;

                    if (attempts > 10)
                    {
                        throw;
                    }

                    //allow the other task to complete
                    await Task.Delay(100).ConfigureAwait(false);
                }
            }
        }

        string GetSubscriptionEntryPath(string eventDir) => Path.Combine(eventDir, endpointName + ".subscription");

        string GetEventDirectory(Type eventType) => Path.Combine(basePath, eventType.FullName);

        string basePath;
        string endpointName;
        string localAddress;
    }
}
