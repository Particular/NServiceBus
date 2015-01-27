namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AcceptanceTesting.Support;
    using NServiceBus.Persistence;

    public static class Persistence
    {
        public static RunDescriptor Default
        {
            get
            {
                var specificPersistence = Environment.GetEnvironmentVariable("Persistence.UseSpecific");

                if (!string.IsNullOrEmpty(specificPersistence))
                {
                    return AllAvailable.Single(r => r.Key == specificPersistence);
                }

                var nonCorePersister = AllAvailable.FirstOrDefault();

                if (nonCorePersister != null)
                {
                    return nonCorePersister;
                }

                return InMemoryPersistenceDescriptor;
            }
        }

        static IEnumerable<RunDescriptor> AllAvailable
        {
            get
            {
                if (availablePersisters == null)
                {
                    availablePersisters = GetAllAvailable().ToList();
                }

                return availablePersisters;
            }
        }

        static Type InMemoryPersistenceType = typeof(InMemoryPersistence);

        static RunDescriptor InMemoryPersistenceDescriptor = new RunDescriptor
        {
            Key = InMemoryPersistenceType.Name,
            Settings =
                new Dictionary<string, string>
                {
                    {"Persistence", InMemoryPersistenceType.AssemblyQualifiedName}
                }
        };

        static IEnumerable<RunDescriptor> GetAllAvailable()
        {
            var foundDefinitions = TypeScanner.GetAllTypesAssignableTo<PersistenceDefinition>()
                .Where(t => t.Assembly != InMemoryPersistenceType.Assembly &&
                t.Assembly != typeof(Persistence).Assembly);

            foreach (var definition in foundDefinitions)
            {
                var key = definition.Name;

                var runDescriptor = new RunDescriptor
                {
                    Key = key,
                    Settings =
                        new Dictionary<string, string>
                                {
                                    {"Persistence", definition.AssemblyQualifiedName}
                                }
                };

                yield return runDescriptor;
            }
        }

        static IList<RunDescriptor> availablePersisters;

    }
}