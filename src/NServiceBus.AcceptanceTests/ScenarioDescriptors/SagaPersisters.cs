namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AcceptanceTesting.Support;
    using Persistence.InMemory.SagaPersister;
    using Persistence.Raven.SagaPersister;
    using Saga;

    public class SagaPersisters : ScenarioDescriptor
    {
        public SagaPersisters()
        {
            var persisters = AvailablePeristers;

            foreach (var persister in persisters)
            {
                Add(new RunDescriptor
                {
                    Key = persister.Name,
                    Settings = new Dictionary<string, string> { { "SagaPersister", persister.AssemblyQualifiedName } }
                });
            }
        }

        static List<Type> AvailablePeristers
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
                var persisters = AvailablePeristers;
                var persister = persisters.FirstOrDefault(p => p != typeof(InMemorySagaPersister) && p != typeof(RavenSagaPersister));

                if (persister == null)
                {
                    persister = typeof(RavenSagaPersister);
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