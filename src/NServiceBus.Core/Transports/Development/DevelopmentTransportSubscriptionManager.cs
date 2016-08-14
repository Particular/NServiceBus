namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Extensibility;
    using Transport;

    class DevelopmentTransportSubscriptionManager : IManageSubscriptions
    {
        public DevelopmentTransportSubscriptionManager(string endpointName, string localAddress)
        {
            this.endpointName = endpointName;
            this.localAddress = localAddress;
            basePath = Path.Combine("c:\\bus", ".events");
        }

        public Task Subscribe(Type eventType, ContextBag context)
        {
            var eventDir = GetEventDirectory(eventType);

            //todo: its probably safer to let the publishers create the dir and have the subscribers require it to be there?
            // that way we can detect that there is indeed a publisher for the event. That said it also means that we will have do "retries" here due to race condition.
            if (!Directory.Exists(eventDir))
            {
                Directory.CreateDirectory(eventDir);
            }

            var subscriptionEntryPath = GetSubscriptionEntryPath(eventDir);

            //add subscription
            File.WriteAllText(subscriptionEntryPath, localAddress);

            return TaskEx.CompletedTask;
        }

        public Task Unsubscribe(Type eventType, ContextBag context)
        {
            var eventDir = GetEventDirectory(eventType);
            var subscriptionEntryPath = GetSubscriptionEntryPath(eventDir);

            if (!File.Exists(subscriptionEntryPath))
            {
                //todo: log a warning/info
                return TaskEx.CompletedTask;
            }

            File.Delete(subscriptionEntryPath);
            return TaskEx.CompletedTask;
        }

        string GetSubscriptionEntryPath(string eventDir)
        {
            var subscriptionEntryPath = Path.Combine(eventDir, endpointName + ".subcription");
            return subscriptionEntryPath;
        }

        string GetEventDirectory(Type eventType)
        {
            var eventId = eventType.FullName;
            var eventDir = Path.Combine(basePath, eventId);
            return eventDir;
        }


        string basePath;
        string endpointName;
        string localAddress;
    }
}