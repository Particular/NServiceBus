namespace NServiceBus.Core.Tests.Sagas;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Sagas;
using NServiceBus.Serialization;
using NUnit.Framework;
using NServiceBus.Persistence;
using NServiceBus.Pipeline;
using NServiceBus.Testing;
using NServiceBus.Unicast.Messages;

[TestFixture]
public class SagaTimeoutManagementTests
{
    [SetUp]
    public void SetUp()
    {
        serializer = new TestSerializer();
        registry = new MessageMetadataRegistry { StrictRegisteredOnlyMode = true };
        registry.RegisterMessageTypeWithHierarchy(typeof(MyTimeout), []);
        registry.Initialize(type => type == typeof(MyTimeout), false);

        saga = new TimeoutSaga();
        persister = new TestPersister { Timeouts = [StoredTimeout("timeout-1", "trigger-1", "stored")] };
        fired = [];
    }

    [Test]
    public async Task Should_fire_the_transported_payload_and_remove_the_stored_timeout()
    {
        var context = CreateContext(timeoutId: "timeout-1-trigger-1", messageId: "message-1");

        await CreateBehavior(persister).Invoke(context, Fire);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fired, Has.Count.EqualTo(1).And.Contains("transported"));
            Assert.That(persister.Timeouts, Is.Empty);
            Assert.That(persister.UpdateCalls, Is.EqualTo(1));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Should_fire_a_timeout_without_the_timeout_id_header_and_leave_stored_state_untouched(bool hasUnrelatedStoredState)
    {
        persister.Timeouts.Clear();
        if (hasUnrelatedStoredState)
        {
            persister.Timeouts.Add(StoredTimeout("timeout-2", "trigger-2", "other"));
        }

        var context = CreateContext(timeoutId: null, messageId: "any");

        await CreateBehavior(persister).Invoke(context, Fire);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fired, Has.Count.EqualTo(1).And.Contains("transported"));
            Assert.That(persister.Timeouts, Has.Count.EqualTo(hasUnrelatedStoredState ? 1 : 0));
            Assert.That(persister.Timeouts.All(t => t.TimeoutId == "timeout-2" && t.TriggerId == "trigger-2"), Is.True);
        }
    }

    [Test]
    public async Task Should_fire_only_the_handler_of_the_exact_stored_timeout_type()
    {
        persister.Timeouts = [StoredTimeout("timeout-1", "trigger-1", "stored", typeof(DerivedTimeout))];
        var behavior = CreateBehavior(persister);

        await behavior.Invoke(CreateContext(timeoutId: "timeout-1-trigger-1", messageId: "message-1", handledTimeoutType: typeof(MyTimeout), message: new DerivedTimeout { Value = "transported" }), Fire);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fired, Is.Empty);
            Assert.That(persister.Timeouts, Has.Count.EqualTo(1));
            Assert.That(persister.GetCalls, Is.Zero);
        }

        await behavior.Invoke(CreateContext(timeoutId: "timeout-1-trigger-1", messageId: "message-1", handledTimeoutType: typeof(DerivedTimeout), message: new DerivedTimeout { Value = "transported" }), Fire);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fired, Has.Count.EqualTo(1).And.Contains("transported"));
            Assert.That(persister.Timeouts, Is.Empty);
        }
    }

    [Test]
    public async Task Should_dispatch_the_payload_requested_even_if_it_changes_afterwards()
    {
        var context = CreateContext(timeoutId: null, messageId: "message-1", isTimeoutMessage: false);

        await CreateBehavior(persister).Invoke(context, _ =>
        {
            saga.RequestThenChangePayload();
            return Task.CompletedTask;
        });

        var sent = context.SentMessages.Single();
        var added = persister.Timeouts.Single(t => t.TimeoutId != "timeout-1");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sent.Message, Is.TypeOf<MyTimeout>().With.Property(nameof(MyTimeout.Value)).EqualTo("requested"));
            Assert.That(sent.Options.GetHeaders()[SagaTimeouts.TimeoutIdHeader], Is.EqualTo($"{added.TimeoutId}-{added.TriggerId}"));
        }
    }

    [Test]
    public async Task Should_dispatch_once_when_a_requested_timeout_is_rescheduled_in_the_same_handler()
    {
        var context = CreateContext(timeoutId: null, messageId: "message-1", isTimeoutMessage: false);

        await CreateBehavior(persister).Invoke(context, _ =>
        {
            saga.RequestThenReschedule();
            return Task.CompletedTask;
        });

        Assert.That(context.SentMessages, Has.Length.EqualTo(1));
    }

    [Test]
    public async Task Should_not_dispatch_when_a_requested_timeout_is_canceled_in_the_same_handler()
    {
        var context = CreateContext(timeoutId: null, messageId: "message-1", isTimeoutMessage: false);

        await CreateBehavior(persister).Invoke(context, _ =>
        {
            saga.RequestThenCancel();
            return Task.CompletedTask;
        });

        Assert.That(context.SentMessages, Is.Empty);
    }

    [Test]
    public async Task Should_ignore_a_timeout_that_is_no_longer_stored()
    {
        persister.Timeouts.Clear();
        var context = CreateContext(timeoutId: "timeout-1-trigger-1", messageId: "message-1");

        await CreateBehavior(persister).Invoke(context, Fire);

        Assert.That(fired, Is.Empty);
    }

    [Test]
    public async Task Should_ignore_a_stale_message_and_keep_the_stored_timeout()
    {
        var context = CreateContext(timeoutId: "timeout-1-old-trigger", messageId: "trigger-1");

        await CreateBehavior(persister).Invoke(context, Fire);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fired, Is.Empty);
            Assert.That(persister.Timeouts.Single().TriggerId, Is.EqualTo("trigger-1"));
        }
    }

    [Test]
    public void Should_fail_when_the_timeout_id_header_is_empty()
    {
        var context = CreateContext(timeoutId: " ", messageId: "message-1");

        _ = Assert.ThrowsAsync<Exception>(() => CreateBehavior(persister).Invoke(context, Fire));
        Assert.That(fired, Is.Empty);
    }

    [Test]
    public void Should_fail_when_the_timeout_id_header_is_present_and_the_persister_does_not_support_timeout_management()
    {
        var context = CreateContext(timeoutId: "timeout-1-trigger-1", messageId: "message-1");

        _ = Assert.ThrowsAsync<Exception>(() => CreateBehavior(new UnsupportedPersister(persister)).Invoke(context, Fire));
        Assert.That(fired, Is.Empty);
    }

    [Test]
    public async Task Should_keep_the_stored_timeout_when_the_handler_fails_so_a_retry_fires_it()
    {
        var behavior = CreateBehavior(persister);

        _ = Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Invoke(CreateContext(timeoutId: "timeout-1-trigger-1", messageId: "message-1"), _ => throw new InvalidOperationException()));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(persister.Timeouts, Has.Count.EqualTo(1));
            Assert.That(persister.UpdateCalls, Is.Zero);
        }

        await behavior.Invoke(CreateContext(timeoutId: "timeout-1-trigger-1", messageId: "retry-message"), Fire);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fired, Has.Count.EqualTo(1).And.Contains("transported"));
            Assert.That(persister.Timeouts, Is.Empty);
        }
    }

    [Test]
    public async Task Should_ignore_the_timeout_id_header_on_a_message_that_is_not_a_timeout()
    {
        var context = CreateContext(timeoutId: "timeout-1-trigger-1", messageId: "message-1", isTimeoutMessage: false);

        await CreateBehavior(new UnsupportedPersister(persister)).Invoke(context, Fire);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fired, Has.Count.EqualTo(1));
            Assert.That(persister.Timeouts, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public async Task Should_dispatch_the_payload_stored_by_the_reschedule_even_if_it_changes_afterwards()
    {
        var context = CreateContext(timeoutId: null, messageId: "message-1", isTimeoutMessage: false);

        await CreateBehavior(persister).Invoke(context, _ =>
        {
            saga.RescheduleThenChangePayload();
            return Task.CompletedTask;
        });

        Assert.That(context.SentMessages.Single().Message, Is.TypeOf<MyTimeout>().With.Property(nameof(MyTimeout.Value)).EqualTo("changed"));
    }

    [Test]
    public async Task Should_reschedule_a_reloaded_timeout_without_find_and_dispatch_the_stored_payload()
    {
        var context = CreateContext(timeoutId: null, messageId: "message-1", isTimeoutMessage: false);

        await CreateBehavior(persister).Invoke(context, _ =>
        {
            saga.RescheduleById("timeout-1");
            return Task.CompletedTask;
        });

        var stored = persister.Timeouts.Single();
        var sent = context.SentMessages.Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(stored.TriggerId, Is.Not.EqualTo("trigger-1"));
            Assert.That(sent.Message, Is.TypeOf<MyTimeout>().With.Property(nameof(MyTimeout.Value)).EqualTo("stored"));
            Assert.That(sent.Options.GetHeaders()[SagaTimeouts.TimeoutIdHeader], Is.EqualTo($"timeout-1-{stored.TriggerId}"));
            Assert.That(stored.TimeoutId, Is.EqualTo("timeout-1"));
        }
    }

    Task Fire(IInvokeHandlerContext context)
    {
        fired.Add(((MyTimeout)context.MessageBeingHandled).Value);
        return Task.CompletedTask;
    }

    SagaPersistenceBehavior CreateBehavior(ISagaPersister sagaPersister)
    {
        var metadata = SagaMetadata.Create<TimeoutSaga>();
        return new SagaPersistenceBehavior(sagaPersister, new DefaultSagaIdGenerator(), [metadata], new TimeoutSerializer(serializer, new MessageDeserializerResolver(serializer, [])), registry, null, null);
    }

    TestableInvokeHandlerContext CreateContext(string timeoutId, string messageId, bool isTimeoutMessage = true, Type handledTimeoutType = null, MyTimeout message = null)
    {
        var headers = new Dictionary<string, string>
        {
            [Headers.SagaId] = persister.Data.Id.ToString(),
            [Headers.SagaType] = typeof(TimeoutSaga).AssemblyQualifiedName
        };

        if (isTimeoutMessage)
        {
            headers[Headers.IsSagaTimeoutMessage] = bool.TrueString;
        }

        if (timeoutId is not null)
        {
            headers[SagaTimeouts.TimeoutIdHeader] = timeoutId;
        }

        return new TestableInvokeHandlerContext
        {
            MessageId = messageId,
            Headers = headers,
            MessageBeingHandled = message ?? new MyTimeout { Value = "transported" },
            MessageMetadata = new MessageMetadata(message?.GetType() ?? typeof(MyTimeout)),
            MessageHandler = new SagaHandler(saga, isTimeoutMessage) { HandlerType = typeof(TimeoutSaga), TimeoutMessageType = handledTimeoutType ?? typeof(MyTimeout) }
        };
    }

    StoredSagaTimeout StoredTimeout(string timeoutId, string triggerId, string value, Type messageType = null)
    {
        using var stream = new MemoryStream();
        serializer.Serialize(new MyTimeout { Value = value }, stream);
        return new StoredSagaTimeout
        {
            TimeoutId = timeoutId,
            TriggerId = triggerId,
            Version = 1,
            DueTime = DateTimeOffset.UtcNow,
            MessageType = (messageType ?? typeof(MyTimeout)).FullName,
            ContentType = serializer.ContentType,
            Payload = stream.ToArray()
        };
    }

    TestSerializer serializer;
    MessageMetadataRegistry registry;
    TimeoutSaga saga;
    TestPersister persister;
    List<string> fired;

    class SagaHandler(object instance, bool isTimeoutHandler) : MessageHandler
    {
        public override object Instance { get; set; } = instance;
        internal override bool IsTimeoutHandler { get; } = isTimeoutHandler;
    }

    class TimeoutSaga : Saga<TimeoutSaga.TimeoutSagaData>, IAmStartedByMessages<Start>, IHandleTimeouts<MyTimeout>, IHandleTimeouts<DerivedTimeout>
    {
        public Task Handle(Start message, IMessageHandlerContext context) => Task.CompletedTask;

        public Task Timeout(MyTimeout state, IMessageHandlerContext context) => Task.CompletedTask;

        public Task Timeout(DerivedTimeout state, IMessageHandlerContext context) => Task.CompletedTask;

        public void RequestThenChangePayload()
        {
            var payload = new MyTimeout { Value = "requested" };
            _ = RequestTimeout(null, DateTimeOffset.UtcNow.AddHours(1), payload);
            payload.Value = "changed after request";
        }

        public void RequestThenReschedule()
        {
            var timeout = Timeouts.Add(new MyTimeout { Value = "requested" }, DateTimeOffset.UtcNow.AddHours(1));
            RescheduleTimeout(timeout.TimeoutId, DateTimeOffset.UtcNow.AddHours(2));
        }

        public void RequestThenCancel()
        {
            var timeout = Timeouts.Add(new MyTimeout { Value = "requested" }, DateTimeOffset.UtcNow.AddHours(1));
            CancelTimeout(timeout.TimeoutId);
        }

        public void RescheduleThenChangePayload()
        {
            var timeout = FindTimeouts<MyTimeout>(_ => true).Single();
            timeout.Payload.Value = "changed";
            RescheduleTimeout(timeout.TimeoutId, DateTimeOffset.UtcNow.AddHours(1));
            timeout.Payload.Value = "changed after reschedule";
        }

        public void RescheduleById(string timeoutId) => RescheduleTimeout(timeoutId, DateTimeOffset.UtcNow.AddHours(1));

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

    class DerivedTimeout : MyTimeout;

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

    class TestPersister : ISagaPersister, ISupportTimeoutManagement
    {
        public TimeoutSaga.TimeoutSagaData Data { get; } = new() { Id = Guid.NewGuid(), Originator = "origin", OriginalMessageId = "id", Key = "key" };
        public List<StoredSagaTimeout> Timeouts { get; set; } = [];
        public int UpdateCalls { get; private set; }
        public int GetCalls { get; private set; }

        public Task<TSagaData> Get<TSagaData>(Guid sagaId, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default) where TSagaData : class, IContainSagaData
        {
            GetCalls++;
            context.SetSagaTimeouts(Data, Timeouts);
            return Task.FromResult((TSagaData)(object)Data);
        }

        public Task Update(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
        {
            UpdateCalls++;
            Timeouts = [.. context.GetSagaTimeouts(sagaData)];
            return Task.CompletedTask;
        }

        public Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default) where TSagaData : class, IContainSagaData => throw new NotImplementedException();

        public Task Complete(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    class UnsupportedPersister(TestPersister inner) : ISagaPersister
    {
        public Task<TSagaData> Get<TSagaData>(Guid sagaId, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default) where TSagaData : class, IContainSagaData => inner.Get<TSagaData>(sagaId, session, context, cancellationToken);

        public Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task Update(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default) where TSagaData : class, IContainSagaData => throw new NotImplementedException();

        public Task Complete(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
