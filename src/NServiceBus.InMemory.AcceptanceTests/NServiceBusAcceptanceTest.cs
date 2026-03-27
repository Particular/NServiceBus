namespace NServiceBus.AcceptanceTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

public partial class NServiceBusAcceptanceTest
{
    static readonly AsyncLocal<InMemoryBroker> currentBroker = new();

    public static InMemoryBroker CurrentBroker =>
        currentBroker.Value ?? throw new InvalidOperationException("No InMemoryBroker available for the current test.");

    [SetUp]
    public void InMemoryTransportSetUp()
    {
        currentBroker.Value = new InMemoryBroker();
    }

    [TearDown]
    public async Task InMemoryTransportTearDown()
    {
        if (currentBroker.Value is { } broker)
        {
            currentBroker.Value = null;
            await broker.DisposeAsync();
        }
    }
}