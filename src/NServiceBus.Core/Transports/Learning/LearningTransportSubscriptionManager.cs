namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Transport;
    using Unicast.Messages;

    class LearningTransportSubscriptionManager : ISubscriptionManager
    {
        public LearningTransportSubscriptionManager(string basePath, string endpointName, string localAddress)
        {
            this.endpointName = endpointName;
            this.localAddress = localAddress;
            this.basePath = Path.Combine(basePath, ".events");
        }

        public Task SubscribeAll(MessageMetadata[] eventTypes, ContextBag context, CancellationToken cancellationToken = default)
        {
            var tasks = new Task[eventTypes.Length];
            for (int i = 0; i < eventTypes.Length; i++)
            {
                tasks[i] = Subscribe(eventTypes[i], cancellationToken);
            }

            return Task.WhenAll(tasks);
        }

        public async Task Unsubscribe(MessageMetadata eventType, ContextBag context, CancellationToken cancellationToken = default)
        {
            var eventDir = GetEventDirectory(eventType.MessageType);
            var subscriptionEntryPath = GetSubscriptionEntryPath(eventDir);

            var attempts = 0;

            // since we have a design that can run into concurrency exceptions we perform a few retries
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

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
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        async Task Subscribe(MessageMetadata eventType, CancellationToken cancellationToken)
        {
            var eventDir = GetEventDirectory(eventType.MessageType);

            // the subscription directory and the subscription information will be created no matter if there's a publisher for the event assuming that the publisher haven’t started yet
            Directory.CreateDirectory(eventDir);

            var subscriptionEntryPath = GetSubscriptionEntryPath(eventDir);

            var attempts = 0;

            // since we have a design that can run into concurrency exceptions we perform a few retries
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await AsyncFile.WriteText(subscriptionEntryPath, localAddress, cancellationToken).ConfigureAwait(false);

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
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
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
