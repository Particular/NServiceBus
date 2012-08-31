namespace NServiceBus.Timeout.Tests
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Hosting.Windows.Persistence;
    using NUnit.Framework;

    public class WithInMemoryTimeoutPersister
    {
        protected IPersistTimeouts persister;

        [SetUp]
        public void SetupContext()
        {
            Configure.GetEndpointNameAction = () => "MyEndpoint";

            persister = new InMemoryTimeoutPersistence();
        }

        public List<Tuple<string, DateTime>> GetNextChunk()
        {
            DateTime nextTimeToRunQuery;
            return persister.GetNextChunk(DateTime.UtcNow.AddYears(3), out nextTimeToRunQuery);
        }
    }
}