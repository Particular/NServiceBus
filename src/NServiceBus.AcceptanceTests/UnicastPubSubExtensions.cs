namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using Configuration.AdvanceExtensibility;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using Settings;

    static class UnicastPubSubExtensions
    {
        public static void RegisterPublisherForType(this EndpointConfiguration config, string publisherEndpoint, Type eventType)
        {
            GetOrCreate<Publishers>(config.GetSettings()).Add(publisherEndpoint, eventType);
        }

        static T GetOrCreate<T>(SettingsHolder settings)
            where T : new()
        {
            T value;
            if (!settings.TryGet(out value))
            {
                value = new T();
                settings.Set<T>(value);
            }
            return value;
        }
    }
}