namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Extensibility;
    using Transport;

    class LearningTransportSubscriptionManager : IManageSubscriptions
    {
        public LearningTransportSubscriptionManager(string basePath, string endpointName, string localAddress)
        {
            this.endpointName = endpointName;
            this.localAddress = localAddress;
            this.basePath = Path.Combine(basePath, ".events");
        }

        public async Task Subscribe(Type eventType, ContextBag context)
        {
            var eventDir = GetEventDirectory(eventType);

            // that way we can detect that there is indeed a publisher for the event. That said it also means that we will have do "retries" here due to race condition.
            Directory.CreateDirectory(eventDir);

            var subscriptionEntryPath = GetSubscriptionEntryPath(eventDir);

            var attempts = 0;

            // since we have a design that can run into concurrency exceptions we perform a few retries
            do
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
            } while (true);
        }

        public async Task Unsubscribe(Type eventType, ContextBag context)
        {
            var eventDir = GetEventDirectory(eventType);
            var subscriptionEntryPath = GetSubscriptionEntryPath(eventDir);


            var attempts = 0;

            // since we have a design that can run into concurrency exceptions we perform a few retries
            do
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
            } while (true);
        }

        string GetSubscriptionEntryPath(string eventDir)
        {
            return Path.Combine(eventDir, endpointName + ".subcription");
        }

        string GetEventDirectory(Type eventType)
        {
            var eventId = eventType.FullName;
            return Path.Combine(basePath, eventId);
        }

        string basePath;
        string endpointName;
        string localAddress;
    }
}