namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System.Collections.Generic;
    using AcceptanceTesting.Support;
    using NServiceBus.SagaPersisters.NHibernate;
    using Persistence.InMemory.SagaPersister;
    using Persistence.Raven.SagaPersister;

    public static class SagaPersisters
    {
        public static readonly RunDescriptor InMemory = new RunDescriptor
            {
                Key = "InMemorySagaPersister",
                Settings =
                    new Dictionary<string, string>
                        {
                            {
                                "SagaPersister",
                                typeof (InMemorySagaPersister).AssemblyQualifiedName
                            }
                        }
            };


        public static readonly RunDescriptor Raven = new RunDescriptor
            {
                Key = "RavenSagaPersister",
                Settings =
                    new Dictionary<string, string>
                        {
                            {
                                "SagaPersister",
                                typeof (RavenSagaPersister).AssemblyQualifiedName
                            }
                        }
            };

        public static readonly RunDescriptor NHibernate = new RunDescriptor
        {
            Key = "NHibernateSagaPersister",
            Settings =
                new Dictionary<string, string>
                        {
                            {
                                "SagaPersister",
                                typeof (SagaPersister).AssemblyQualifiedName
                            }
                        }
        };
    }
}