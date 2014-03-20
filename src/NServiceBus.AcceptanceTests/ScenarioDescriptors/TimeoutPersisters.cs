﻿namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AcceptanceTesting.Support;
    using Persistence.InMemory.TimeoutPersister;
    using Persistence.Raven.TimeoutPersister;
    using Timeout.Core;

    public class TimeoutPersisters : ScenarioDescriptor
    {
        public TimeoutPersisters()
        {
            var persisters = AvailablePersisters;

            foreach (var persister in persisters)
            {
                Add(new RunDescriptor
                {
                    Key = persister.Name,
                    Settings = new Dictionary<string, string> { { "TimeoutPersister", persister.AssemblyQualifiedName } }
                });
            }
        }

        static List<Type> AvailablePersisters
        {
            get
            {
                var persisters = TypeScanner.GetAllTypesAssignableTo<IPersistTimeouts>()
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
                var persister = persisters.FirstOrDefault(p => p != typeof(InMemoryTimeoutPersistence) && p != typeof(RavenTimeoutPersistence));

                if (persister == null)
                {
                    persister = typeof(InMemoryTimeoutPersistence);
                }

                return new RunDescriptor
                {
                    Key = persister.Name,
                    Settings = new Dictionary<string, string>
                    {
                        {"TimeoutPersister", persister.AssemblyQualifiedName}
                    }
                };
            }
        }
    }

}