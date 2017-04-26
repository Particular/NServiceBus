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

        public Task Subscribe(Type eventType, ContextBag context)
        {
            var eventDir = GetEventDirectory(eventType);

            // that way we can detect that there is indeed a publisher for the event. That said it also means that we will have do "retries" here due to race condition.
            Directory.CreateDirectory(eventDir);

            var subscriptionEntryPath = GetSubscriptionEntryPath(eventDir);

            //add subscription
            return AsyncFile.WriteText(subscriptionEntryPath, localAddress);
        }

        public Task Unsubscribe(Type eventType, ContextBag context)
        {
            var eventDir = GetEventDirectory(eventType);
            var subscriptionEntryPath = GetSubscriptionEntryPath(eventDir);

            if (!File.Exists(subscriptionEntryPath))
            {
                return TaskEx.CompletedTask;
            }

            File.Delete(subscriptionEntryPath);
            return TaskEx.CompletedTask;
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