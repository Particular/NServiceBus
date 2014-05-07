namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AcceptanceTesting.Support;
    using Persistence.InMemory.SagaPersister;
    using Saga;

    public class SagaPersisters : ScenarioDescriptor
    {
        public SagaPersisters()
        {
            var persisters = AvailablePersisters;

            foreach (var persister in persisters)
            {
                Add(new RunDescriptor
                {
                    Key = persister.Name,
                    Settings = new Dictionary<string, string> { { "SagaPersister", persister.AssemblyQualifiedName } }
                });
            }
        }

        static List<Type> AvailablePersisters
        {
            get
            {
                var persisters = TypeScanner.GetAllTypesAssignableTo<ISagaPersister>()
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
                var persister = persisters.FirstOrDefault(p => p != typeof(InMemorySagaPersister));

                if (persister == null)
                {
                    persister = typeof(InMemorySagaPersister);
                }

                return new RunDescriptor
                {
                    Key = persister.Name,
                    Settings = new Dictionary<string, string>
                    {
                        {"SagaPersister", persister.AssemblyQualifiedName}
                    }
                };
            }
        }
    }
}