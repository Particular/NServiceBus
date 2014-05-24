namespace NServiceBus.Core.Tests.Timeout
{
    using InMemory.TimeoutPersister;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;

    [TestFixture]
    public class When_fetching_timeouts_from_storage_with_inMemory : When_fetching_timeouts_from_storage
    {
        protected override IPersistTimeouts CreateTimeoutPersister()
        {
            return new InMemoryTimeoutPersister();
        }
    }
}