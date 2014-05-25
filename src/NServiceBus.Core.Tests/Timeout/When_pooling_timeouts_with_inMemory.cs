namespace NServiceBus.Core.Tests.Timeout
{
    using InMemory.TimeoutPersister;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;

    [TestFixture]
    [Explicit]
    class When_pooling_timeouts_with_inMemory : When_pooling_timeouts
    {
        protected override IPersistTimeouts CreateTimeoutPersister()
        {
            return new InMemoryTimeoutPersister();
        }
    }
}