namespace NServiceBus.Core.Tests.Sagas;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NServiceBus.Extensibility;
using NServiceBus.Hosting;
using NServiceBus.Outbox;
using NServiceBus.Persistence;
using NServiceBus.Sagas;
using NServiceBus.Serialization;
using NServiceBus.Transport;
using NServiceBus.Unicast.Messages;
using NUnit.Framework;
using OutboxTransportOperation = NServiceBus.Outbox.TransportOperation;

[TestFixture]
public class SagaTimeoutManagementSatelliteTests
{
    [SetUp]
    public void SetUp()
    {
        serviceProvider = new ServiceCollection().BuildServiceProvider();
        persister = new TestPersister();
        dispatcher = new RecordingDispatcher();
        outboxStorage = new TestOutboxStorage();
    }

    [TearDown]
    public void TearDown() => serviceProvider.Dispose();

    [Test]
    public async Task Should_store_the_operations_in_the_outbox_before_dispatching_them()
    {
        await CreateSatellite().Handle(serviceProvider, CreateRequest("request-1", expectedVersion: 1));

        var stored = outboxStorage.Stored.Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(stored.MessageId, Is.EqualTo("request-1"));
            Assert.That(outboxStorage.Committed, Is.True);
            Assert.That(persister.UpdateCalls, Is.EqualTo(1));
            Assert.That(persister.Timeouts.Single().Version, Is.EqualTo(2));
            Assert.That(dispatcher.Dispatched.Select(o => o.Message.MessageId), Is.EqualTo(stored.TransportOperations.Select(o => o.MessageId)));
            Assert.That(EnclosedTypes(dispatcher.Dispatched), Is.EquivalentTo(RescheduleOperations));
            Assert.That(outboxStorage.Dispatched.Single(), Is.EqualTo("request-1"));
        }
    }

    [Test]
    public async Task Should_dispatch_the_stored_operations_of_a_duplicate_without_applying_it_again()
    {
        var storedOperation = new OutboxTransportOperation("stored-1", new DispatchProperties { ["Destination"] = "Endpoint" }, Array.Empty<byte>(), []);
        outboxStorage.Existing = new OutboxMessage("request-1", [storedOperation]);

        await CreateSatellite().Handle(serviceProvider, CreateRequest("request-1", expectedVersion: 1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(persister.GetCalls, Is.Zero);
            Assert.That(outboxStorage.Stored, Is.Empty);
            Assert.That(dispatcher.Dispatched.Single().Message.MessageId, Is.EqualTo("stored-1"));
            Assert.That(dispatcher.Dispatched.Single().Destination, Is.EqualTo("Endpoint"));
            Assert.That(outboxStorage.Dispatched.Single(), Is.EqualTo("request-1"));
        }
    }

    [Test]
    public async Task Should_store_a_rejection_without_saving_the_saga()
    {
        await CreateSatellite().Handle(serviceProvider, CreateRequest("request-1", expectedVersion: 5));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(persister.UpdateCalls, Is.Zero);
            Assert.That(outboxStorage.Committed, Is.True);
            Assert.That(EnclosedTypes(dispatcher.Dispatched).Single(), Is.EqualTo("NServiceBus.SagaTimeoutChangeRejected"));
            Assert.That(outboxStorage.Stored.Single().TransportOperations, Has.Length.EqualTo(1));
        }
    }

    SagaTimeoutManagementSatellite CreateSatellite()
    {
        var sagas = new SagaMetadataCollection { SagaMetadata.Create<TimeoutSaga>() };

        var registry = new MessageMetadataRegistry();
        registry.RegisterMessageTypeWithHierarchy(typeof(MyTimeout), []);
        registry.Initialize(type => type == typeof(MyTimeout), false);

        var hostInformation = new HostInformation(Guid.NewGuid(), "host");
        var connection = new MessagingBasedServicePlatformConnection(new MessagingBasedServicePlatformConnection.Configuration { ServiceControlQueue = "Particular.ServiceControl" }, dispatcher);
        var notifier = new SagaTimeoutPlatformNotifier(connection, "Endpoint", hostInformation, "Endpoint.SagaTimeouts");
        var serializer = new TestSerializer();

        return new SagaTimeoutManagementSatellite(persister, persister, sagas, registry, new TimeoutSerializer(serializer, new MessageDeserializerResolver(serializer, [])), dispatcher, outboxStorage, notifier, "Endpoint", hostInformation, "Endpoint", NullLogger<SagaTimeoutManagementSatellite>.Instance);
    }

    MessageContext CreateRequest(string messageId, int expectedVersion)
    {
        var headers = new Dictionary<string, string>
        {
            [Headers.MessageId] = messageId,
            [SagaTimeoutManagementHeaders.OperationId] = Guid.NewGuid().ToString(),
            [SagaTimeoutManagementHeaders.SagaType] = typeof(TimeoutSaga).FullName,
            [SagaTimeoutManagementHeaders.SagaId] = persister.Data.Id.ToString(),
            [SagaTimeoutManagementHeaders.TimeoutId] = "timeout-1",
            [SagaTimeoutManagementHeaders.ExpectedVersion] = expectedVersion.ToString(),
            [SagaTimeoutManagementHeaders.Change] = SagaTimeoutManagementHeaders.RescheduleChange,
            [SagaTimeoutManagementHeaders.DueTime] = DateTimeOffsetHelper.ToWireFormattedString(DateTimeOffset.UtcNow.AddHours(1))
        };

        return new MessageContext("native-id", headers, ReadOnlyMemory<byte>.Empty, new TransportTransaction(), "Endpoint.SagaTimeouts", new ContextBag());
    }

    static IEnumerable<string> EnclosedTypes(IEnumerable<UnicastTransportOperation> operations) =>
        operations.Select(o => o.Message.Headers[Headers.EnclosedMessageTypes].Split(',')[0]);

    static readonly string[] RescheduleOperations = [typeof(MyTimeout).FullName, "NServiceBus.SagaTimeoutRescheduled"];

    ServiceProvider serviceProvider;
    TestPersister persister;
    RecordingDispatcher dispatcher;
    TestOutboxStorage outboxStorage;

    class TimeoutSaga : Saga<TimeoutSaga.TimeoutSagaData>, IAmStartedByMessages<Start>, IHandleTimeouts<MyTimeout>
    {
        public Task Handle(Start message, IMessageHandlerContext context) => Task.CompletedTask;

        public Task Timeout(MyTimeout state, IMessageHandlerContext context) => Task.CompletedTask;

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TimeoutSagaData> mapper) => mapper.MapSaga(s => s.Key).ToMessage<Start>(m => m.Key);

        public class TimeoutSagaData : ContainSagaData
        {
            public string Key { get; set; }
        }
    }

    class Start
    {
        public string Key { get; set; }
    }

    class MyTimeout
    {
        public string Value { get; set; }
    }

    class TestSerializer : IMessageSerializer
    {
        public string ContentType => "test/json";

        public void Serialize(object message, Stream stream) => JsonSerializer.Serialize(stream, message, message.GetType());

        public object[] Deserialize(ReadOnlyMemory<byte> body, IList<Type> messageTypes = null) => [JsonSerializer.Deserialize(body.Span, messageTypes[0])];
    }

    class TestPersister : ISagaPersister, ITimeoutPersister
    {
        public TimeoutSaga.TimeoutSagaData Data { get; } = new() { Id = Guid.NewGuid(), Originator = "origin", OriginalMessageId = "id", Key = "key" };

        public List<StoredSagaTimeout> Timeouts { get; set; } =
        [
            new()
            {
                TimeoutId = "timeout-1",
                TriggerId = "trigger-1",
                Version = 1,
                DueTime = DateTimeOffset.UtcNow,
                MessageType = typeof(MyTimeout).FullName,
                ContentType = "test/json",
                Payload = JsonSerializer.SerializeToUtf8Bytes(new MyTimeout { Value = "stored" })
            }
        ];

        public int UpdateCalls { get; private set; }
        public int GetCalls { get; private set; }

        public Task<TSagaData> Get<TSagaData>(Guid sagaId, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default) where TSagaData : class, IContainSagaData
        {
            GetCalls++;
            return Task.FromResult((TSagaData)(object)Data);
        }

        public Task Update(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
        {
            UpdateCalls++;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<StoredSagaTimeout>> Get(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StoredSagaTimeout>>([.. Timeouts]);

        public Task Request(IContainSagaData sagaData, StoredSagaTimeout timeout, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
        {
            Timeouts.Add(timeout);
            return Task.CompletedTask;
        }

        public Task Reschedule(IContainSagaData sagaData, StoredSagaTimeout timeout, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
        {
            Timeouts[Timeouts.FindIndex(t => t.TimeoutId == timeout.TimeoutId)] = timeout;
            return Task.CompletedTask;
        }

        public Task MarkFired(IContainSagaData sagaData, string timeoutId, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default) =>
            Cancel(sagaData, timeoutId, session, context, cancellationToken);

        public Task Cancel(IContainSagaData sagaData, string timeoutId, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
        {
            _ = Timeouts.RemoveAll(t => t.TimeoutId == timeoutId);
            return Task.CompletedTask;
        }

        public Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default) where TSagaData : class, IContainSagaData => throw new NotImplementedException();

        public Task Complete(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    class RecordingDispatcher : IMessageDispatcher
    {
        public List<UnicastTransportOperation> Dispatched { get; } = [];

        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken = default)
        {
            Dispatched.AddRange(outgoingMessages.UnicastTransportOperations);
            return Task.CompletedTask;
        }
    }

    class TestOutboxStorage : IOutboxStorage
    {
        public OutboxMessage Existing { get; set; }
        public List<OutboxMessage> Stored { get; } = [];
        public List<string> Dispatched { get; } = [];
        public bool Committed { get; private set; }

        public Task<OutboxMessage> Get(string messageId, ContextBag context, CancellationToken cancellationToken = default) => Task.FromResult(Existing);

        public Task Store(OutboxMessage message, IOutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken = default)
        {
            Stored.Add(message);
            return Task.CompletedTask;
        }

        public Task SetAsDispatched(string messageId, ContextBag context, CancellationToken cancellationToken = default)
        {
            Dispatched.Add(messageId);
            return Task.CompletedTask;
        }

        public Task<IOutboxTransaction> BeginTransaction(ContextBag context, CancellationToken cancellationToken = default) => Task.FromResult<IOutboxTransaction>(new Transaction(this));

        sealed class Transaction(TestOutboxStorage storage) : IOutboxTransaction
        {
            public Task Commit(CancellationToken cancellationToken = default)
            {
                storage.Committed = true;
                return Task.CompletedTask;
            }

            public void Dispose()
            {
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
