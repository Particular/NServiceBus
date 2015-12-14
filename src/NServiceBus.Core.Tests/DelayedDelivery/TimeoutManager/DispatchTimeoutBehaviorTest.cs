namespace NServiceBus.Core.Tests.Timeout.TimeoutManager
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Timeout.Core;
    using NServiceBus.Transports;
    using NUnit.Framework;

    public class DispatchTimeoutBehaviorTest
    {
        [Test]
        public async Task Invoke_when_message_dispatched_should_remove_timeout_from_timeout_storage()
        {
            var messageDispatcher = new FakeMessageDispatcher();
            var timeoutPersister = new InMemoryTimeoutPersister();
            var behavior = new DispatchTimeoutBehavior(messageDispatcher, timeoutPersister, TransportTransactionMode.TransactionScope);
            var timeoutData = CreateTimeout();
            await timeoutPersister.Add(timeoutData, null);

            await behavior.Invoke(CreateContext(timeoutData.Id), context => TaskEx.Completed);

            var result = await timeoutPersister.Peek(timeoutData.Id, null);
            Assert.Null(result);
        }

        [Test]
        public async Task Invoke_when_timeout_not_in_storage_should_process_successfully()
        {
            var messageDispatcher = new FakeMessageDispatcher();
            var timeoutPersister = new InMemoryTimeoutPersister();
            var behavior = new DispatchTimeoutBehavior(messageDispatcher, timeoutPersister, TransportTransactionMode.TransactionScope);

            await behavior.Invoke(CreateContext(Guid.NewGuid().ToString()), context => TaskEx.Completed);

            Assert.AreEqual(0, messageDispatcher.OutgoingMessages.Count());
        }

        [Test]
        public async Task Invoke_when_dispatching_message_fails_should_keep_timeout_in_storage()
        {
            var messageDispatcher = new FailingMessageDispatcher();
            var timeoutPersister = new InMemoryTimeoutPersister();
            var behavior = new DispatchTimeoutBehavior(messageDispatcher, timeoutPersister, TransportTransactionMode.TransactionScope);
            var timeoutData = CreateTimeout();
            await timeoutPersister.Add(timeoutData, null);

            Assert.Throws<Exception>(async () => await behavior.Invoke(CreateContext(timeoutData.Id), context => TaskEx.Completed));

            var result = await timeoutPersister.Peek(timeoutData.Id, null);
            Assert.NotNull(result);
        }

        [Test]
        public void Invoke_when_removing_timeout_fails_should_throw_exception()
        {
            var messageDispatcher = new FakeMessageDispatcher();
            var timeoutPersister = new FakeTimeoutStorage
            {
                OnPeek = (id, bag) => CreateTimeout(),
                OnTryRemove = (id, bag) => false // simulates a concurrent delete
            };

            var behavior = new DispatchTimeoutBehavior(messageDispatcher, timeoutPersister, TransportTransactionMode.TransactionScope);

            Assert.Throws<Exception>(async () => await behavior.Invoke(CreateContext(Guid.NewGuid().ToString()), context => TaskEx.Completed));
        }

        [Test]
        public async Task Invoke_when_using_dtc_should_enlist_dispatch_in_transaction()
        {
            var messageDispatcher = new FakeMessageDispatcher();
            var timeoutPersister = new InMemoryTimeoutPersister();
            var behavior = new DispatchTimeoutBehavior(messageDispatcher, timeoutPersister, TransportTransactionMode.TransactionScope);
            var timeoutData = CreateTimeout();
            await timeoutPersister.Add(timeoutData, null);

            await behavior.Invoke(CreateContext(timeoutData.Id), context => TaskEx.Completed);

            var transportOperation = messageDispatcher.OutgoingMessages.Single();
            Assert.AreEqual(DispatchConsistency.Default, transportOperation.DispatchOptions.RequiredDispatchConsistency);
        }

        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.None)]
        public async Task Invoke_when_not_using_dtc_transport_should_not_enlist_dispatch_in_transaction(TransportTransactionMode nonDtcTxSettings)
        {
            var messageDispatcher = new FakeMessageDispatcher();
            var timeoutPersister = new InMemoryTimeoutPersister();
            var behavior = new DispatchTimeoutBehavior(messageDispatcher, timeoutPersister, nonDtcTxSettings);
            var timeoutData = CreateTimeout();
            await timeoutPersister.Add(timeoutData, null);

            await behavior.Invoke(CreateContext(timeoutData.Id), context => TaskEx.Completed);

            var transportOperation = messageDispatcher.OutgoingMessages.Single();
            Assert.AreEqual(DispatchConsistency.Isolated, transportOperation.DispatchOptions.RequiredDispatchConsistency);
        }

        static TimeoutData CreateTimeout()
        {
            return new TimeoutData
            {
                Destination = "endpointQueue",
                Headers = new Dictionary<string, string>()
            };
        }

        static IIncomingPhysicalMessageContext CreateContext(string timeoutId)
        {
            var messageId = Guid.NewGuid().ToString("D");
            var headers = new Dictionary<string, string>
            {
                {"Timeout.Id", timeoutId}
            };

            return new IncomingPhysicalMessageContext(
                new IncomingMessage(messageId, headers, new MemoryStream()), null);
        }

        class FakeMessageDispatcher : IDispatchMessages
        {
            public IEnumerable<TransportOperation> OutgoingMessages { get; private set; } = Enumerable.Empty<TransportOperation>();

            public Task Dispatch(IEnumerable<TransportOperation> outgoingMessages, ContextBag context)
            {
                OutgoingMessages = outgoingMessages;
                return TaskEx.Completed;
            }
        }

        class FailingMessageDispatcher : IDispatchMessages
        {
            public Task Dispatch(IEnumerable<TransportOperation> outgoingMessages, ContextBag context)
            {
                throw new Exception("simulated exception");
            }
        }

        class FakeTimeoutStorage : IPersistTimeouts
        {
            public Task Add(TimeoutData timeout, ContextBag context)
            {
                return TaskEx.Completed;
            }

            public Task<bool> TryRemove(string timeoutId, ContextBag context)
            {
                return Task.FromResult(OnTryRemove(timeoutId, context));
            }

            public Task<TimeoutData> Peek(string timeoutId, ContextBag context)
            {
                return Task.FromResult(OnPeek(timeoutId, context));
            }

            public Task RemoveTimeoutBy(Guid sagaId, ContextBag context)
            {
                return TaskEx.Completed;
            }

            public Func<string, ContextBag, bool> OnTryRemove { get; set; } = (id, bag) => true;
            public Func<string, ContextBag, TimeoutData> OnPeek { get; set; } = (id, bag) => null;
        }
    }
}