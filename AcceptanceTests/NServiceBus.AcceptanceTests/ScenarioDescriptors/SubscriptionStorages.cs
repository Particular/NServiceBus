namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System.Collections.Generic;
    using AcceptanceTesting.Support;
    using Persistence.InMemory.SubscriptionStorage;
    using Persistence.Msmq.SubscriptionStorage;
    using Persistence.Raven.SubscriptionStorage;
    using Unicast.Subscriptions.NHibernate;

    public static class SubscriptionStorages
    {
        public static readonly RunDescriptor InMemory = new RunDescriptor
            {
                Key = "InMemorySubscriptionStorage",
                Settings =
                    new Dictionary<string, string>
                        {
                            {
                                "SubscriptionStorage",
                                typeof (InMemorySubscriptionStorage).AssemblyQualifiedName
                            }
                        }
            };


        public static readonly RunDescriptor Raven = new RunDescriptor
            {
                Key = "RavenSubscriptionStorage",
                Settings =
                    new Dictionary<string, string>
                        {
                            {
                                "SubscriptionStorage",
                                typeof (RavenSubscriptionStorage).AssemblyQualifiedName
                            }
                        }
            };

        public static readonly RunDescriptor NHibernate = new RunDescriptor
            {
                Key = "NHibernateSubscriptionStorage",
                Settings =
                    new Dictionary<string, string>
                        {
                            {
                                "SubscriptionStorage",
                                typeof (SubscriptionStorage).AssemblyQualifiedName
                            }
                        }
            };

        public static readonly RunDescriptor Msmq = new RunDescriptor
            {
                Key = "MsmqSubscriptionStorage",
                Settings =
                    new Dictionary<string, string>
                        {
                            {
                                "SubscriptionStorage",
                                typeof (MsmqSubscriptionStorage).AssemblyQualifiedName
                            }
                        }
            };
    }
}