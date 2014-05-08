namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AcceptanceTesting.Support;
    using Persistence.InMemory.SubscriptionStorage;
    using Persistence.Msmq.SubscriptionStorage;
    using Persistence.Raven.SubscriptionStorage;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    public class SubscriptionPersisters : ScenarioDescriptor
    {
        public SubscriptionPersisters()
        {
            var persisters = AvailablePersisters;

            foreach (var persister in persisters)
            {
                Add(new RunDescriptor
                {
                    Key = persister.Name,
                    Settings = new Dictionary<string, string> { { "SubscriptionStorage", persister.AssemblyQualifiedName } }
                });
            }
        }

        static List<Type> AvailablePersisters
        {
            get
            {
                var persisters = TypeScanner.GetAllTypesAssignableTo<ISubscriptionStorage>()
                    .Where(t => !t.IsInterface)
                    .ToList();
                return persisters;
            }
        }

        public static RunDescriptor Default
        {
            get
            {
                var persisters = AvailablePersisters;
#pragma warning disable 612
                var persister = persisters.FirstOrDefault(p => p != typeof(RavenSubscriptionStorage) && p != typeof(MsmqSubscriptionStorage));
#pragma warning restore 612

                if (persister == null)
                {
                    persister = typeof(InMemorySubscriptionStorage);
                }

                return new RunDescriptor
                {
                    Key = persister.Name,
                    Settings = new Dictionary<string, string>
                    {
                        {"SubscriptionStorage", persister.AssemblyQualifiedName}
                    }
                };
            }
        }
    }
}