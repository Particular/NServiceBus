namespace NServiceBus.Timeout.Tests
{
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
    }
}