﻿namespace NServiceBus.Core.Tests.Sagas;

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

        var messageConventions = new Conventions();

        messageConventions.DefineCommandTypeConventions(t => t == messageType);

        var sagaMetadata = SagaMetadata.Create(typeof(TestSaga), availableTypes, messageConventions);

        if (!sagaMetadata.TryGetFinder(messageType.FullName, out var finderDefinition))
        {
            throw new Exception("Finder not found");
        }

        var services = new ServiceCollection();
        services.AddTransient(sp => new ReturnsNullFinder());

        var customerFinderAdapter = new CustomFinderAdapter<TestSaga.SagaData, StartSagaMessage>();

        using var serviceProvider = services.BuildServiceProvider();
        Assert.That(async () => await customerFinderAdapter.Find(serviceProvider, finderDefinition, new FakeSynchronizedStorageSession(), new ContextBag(), new StartSagaMessage(), new Dictionary<string, string>()),
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
    }

    public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }
}

class StartSagaMessage
{ }

class ReturnsNullFinder : ISagaFinder<TestSaga.SagaData, StartSagaMessage>
{
    public Task<TestSaga.SagaData> FindBy(StartSagaMessage message, ISynchronizedStorageSession storageSession, IReadOnlyContextBag context, CancellationToken cancellationToken = default)
    {
        return null;
    }
}
