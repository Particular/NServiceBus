namespace NServiceBus.Core.Tests.Sagas;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Fakes;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Persistence;
using NServiceBus.Sagas;
using NUnit.Framework;

[TestFixture]
public class CustomFinderAdapterTests
{
    [Test]
    public void Throws_friendly_exception_when_ISagaFinder_FindBy_returns_null()
    {
        var availableTypes = new List<Type>
        {
            typeof(ReturnsNullFinder)
        };

        var messageType = typeof(StartSagaMessage);

        var sagaMetadata = SagaMetadata.Create(typeof(TestSaga));

        if (!sagaMetadata.TryGetFinder(messageType.FullName, out var finderDefinition))
        {
            throw new Exception("Finder not found");
        }

        var services = new ServiceCollection();
        services.AddTransient(sp => new ReturnsNullFinder());

        var customerFinderAdapter = new CustomFinderAdapter<ReturnsNullFinder, TestSaga.SagaData, StartSagaMessage>();

        using var serviceProvider = services.BuildServiceProvider();
        Assert.That(async () => await customerFinderAdapter.Find(serviceProvider, new FakeSynchronizedStorageSession(), new ContextBag(), new StartSagaMessage(), new Dictionary<string, string>()),
            Throws.Exception.With.Message.EqualTo("Return a Task or mark the method as async."));
    }
}

class TestSaga : Saga<TestSaga.SagaData>, IAmStartedByMessages<StartSagaMessage>
{
    internal class SagaData : ContainSagaData
    {
    }

    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
    {
        mapper.ConfigureFinderMapping<StartSagaMessage, ReturnsNullFinder>();
    }

    public Task Handle(StartSagaMessage message, IMessageHandlerContext context) => Task.CompletedTask;
}

class StartSagaMessage
{ }

class ReturnsNullFinder : ISagaFinder<TestSaga.SagaData, StartSagaMessage>
{
    public Task<TestSaga.SagaData> FindBy(StartSagaMessage message, ISynchronizedStorageSession storageSession, IReadOnlyContextBag context, CancellationToken cancellationToken = default) => null;
}
