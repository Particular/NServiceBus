namespace NServiceBus.Core.Tests.Sagas
{
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Persistence;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.Extensions.DependencyInjection;

    [TestFixture]
    public class CustomFinderAdapterTests
    {
        [Test]
        public void Throws_friendly_exception_when_IFindSagas_FindBy_returns_null()
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

            Assert.That(async () => await customerFinderAdapter.Find(services.BuildServiceProvider(), finderDefinition, new InMemorySynchronizedStorageSession(), new ContextBag(), new StartSagaMessage(), new Dictionary<string, string>(), CancellationToken.None),
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

        public Task Handle(StartSagaMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    class StartSagaMessage
    { }

    class ReturnsNullFinder : IFindSagas<TestSaga.SagaData>.Using<StartSagaMessage>
    {
        public Task<TestSaga.SagaData> FindBy(StartSagaMessage message, SynchronizedStorageSession storageSession, ReadOnlyContextBag context, CancellationToken cancellationToken)
        {
            return null;
        }
    }
}
