namespace NServiceBus.AcceptanceTests;

using System.Collections.Concurrent;
using NUnit.Framework;

public partial class NServiceBusAcceptanceTest
{
    [SetUp]
    public void InMemoryTransportSetUp()
    {
        var testId = TestContext.CurrentContext.Test.ID;
        Brokers[testId] = new InMemoryBroker();
    }

    internal static ConcurrentDictionary<string, InMemoryBroker> Brokers = [];
}
