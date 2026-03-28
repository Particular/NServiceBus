namespace NServiceBus.AcceptanceTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

public partial class NServiceBusAcceptanceTest
{
    static readonly AsyncLocal<InMemoryBroker> currentBroker = new();
    static InMemoryBroker sharedBroker;

    public static InMemoryBroker CurrentBroker =>
        currentBroker.Value ?? sharedBroker ?? throw new InvalidOperationException("No InMemoryBroker available for the current test.");

    [SetUp]
    public void InMemoryTransportSetUp()
    {
        var broker = new InMemoryBroker();
        currentBroker.Value = broker;
        sharedBroker = broker;
    }

    [TearDown]
    public async Task InMemoryTransportTearDown()
    {
        if (currentBroker.Value is { } broker)
        {
            currentBroker.Value = null;
            if (ReferenceEquals(sharedBroker, broker))
            {
                sharedBroker = null;
            }
            await broker.DisposeAsync();
        }
    }
}