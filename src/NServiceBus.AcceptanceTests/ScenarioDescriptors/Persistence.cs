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
                var specificPersistence = EnvironmentHelper.GetEnvironmentVariable("Persistence.UseSpecific");
                var runDescriptors = AllAvailable;

                if (!string.IsNullOrEmpty(specificPersistence))
                {
                    return runDescriptors.Single(r => r.Key == specificPersistence);
                }

                var nonCorePersister = runDescriptors.FirstOrDefault();

                if (nonCorePersister != null)
                {
                    return nonCorePersister;
                }

                var inMemory = new RunDescriptor(InMemoryPersistenceType.Name);
                inMemory.Settings.Set("Persistence", InMemoryPersistenceType);
                return inMemory;
            }
        }

        static IEnumerable<RunDescriptor> AllAvailable
        {
            get
            {
                foreach (var definition in foundDefinitions.Value)
                {
                    var key = definition.Name;

                    var runDescriptor = new RunDescriptor(key);
                    runDescriptor.Settings.Set("Persistence", definition);

                    var connectionString = Environment.GetEnvironmentVariable(key + ".ConnectionString");

                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        runDescriptor.Settings.Set("Persistence.ConnectionString", connectionString);
                    }

                    yield return runDescriptor;
                }
            }
        }

        static Type InMemoryPersistenceType = typeof(InMemoryPersistence);

        static Lazy<List<Type>> foundDefinitions = new Lazy<List<Type>>(() =>
        {
            return TypeScanner.GetAllTypesAssignableTo<PersistenceDefinition>()
                .Where(t => t.Assembly != InMemoryPersistenceType.Assembly &&
                            t.Assembly != typeof(Persistence).Assembly)
                .ToList();
        });
    }
}