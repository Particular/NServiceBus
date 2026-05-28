namespace NServiceBus.Core.Tests.Sagas;

using System;
using System.Threading.Tasks;
using NServiceBus.Sagas;
using NUnit.Framework;
using Testing;

[TestFixture]
public class AttachSagaDetailsToOutGoingMessageBehaviorTests
{
    [Test]
    public async Task Should_set_originating_saga_headers_when_active_saga_exists_and_found()
    {
        var saga = CreateActiveSagaInstance("saga-id");
        var context = new TestableOutgoingLogicalMessageContext();
        context.Extensions.Set(saga);

        await new AttachSagaDetailsToOutGoingMessageBehavior().Invoke(context, _ => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Headers[Headers.OriginatingSagaId], Is.EqualTo("saga-id"));
            Assert.That(context.Headers[Headers.OriginatingSagaType], Is.EqualTo(typeof(MySaga).AssemblyQualifiedName));
        }
    }

    [Test]
    public async Task Should_not_set_headers_when_active_saga_is_missing()
    {
        var context = new TestableOutgoingLogicalMessageContext();

        await new AttachSagaDetailsToOutGoingMessageBehavior().Invoke(context, _ => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Headers.ContainsKey(Headers.OriginatingSagaId), Is.False);
            Assert.That(context.Headers.ContainsKey(Headers.OriginatingSagaType), Is.False);
        }
    }

    [Test]
    public async Task Should_not_set_headers_when_active_saga_is_not_found()
    {
        var saga = CreateActiveSagaInstance("saga-id");
        saga.MarkAsNotFound();
        var context = new TestableOutgoingLogicalMessageContext();
        context.Extensions.Set(saga);

        await new AttachSagaDetailsToOutGoingMessageBehavior().Invoke(context, _ => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Headers.ContainsKey(Headers.OriginatingSagaId), Is.False);
            Assert.That(context.Headers.ContainsKey(Headers.OriginatingSagaType), Is.False);
        }
    }

    [TestCase(null)]
    [TestCase("")]
    public async Task Should_not_set_headers_when_saga_id_is_null_or_empty(string sagaId)
    {
        var saga = CreateActiveSagaInstance(sagaId);
        var context = new TestableOutgoingLogicalMessageContext();
        context.Extensions.Set(saga);

        await new AttachSagaDetailsToOutGoingMessageBehavior().Invoke(context, _ => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Headers.ContainsKey(Headers.OriginatingSagaId), Is.False);
            Assert.That(context.Headers.ContainsKey(Headers.OriginatingSagaType), Is.False);
        }
    }

    static ActiveSagaInstance CreateActiveSagaInstance(string sagaId)
    {
        var saga = new MySaga();
        var activeSagaInstance = new ActiveSagaInstance(saga, SagaMetadata.Create<MySaga>(), () => DateTimeOffset.UtcNow)
        {
            SagaId = sagaId
        };
        return activeSagaInstance;
    }

    class MySaga : Saga<MySagaData>, IAmStartedByMessages<StartingMessage>
    {
        public Task Handle(StartingMessage message, IMessageHandlerContext context) => Task.CompletedTask;

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper)
        {
            mapper.MapSaga(s => s.Correlation).ToMessage<StartingMessage>(m => m.Correlation);
        }
    }

    class StartingMessage : IMessage
    {
        public string Correlation { get; set; }
    }

    class MySagaData : ContainSagaData
    {
        public string Correlation { get; set; }
    }
}
