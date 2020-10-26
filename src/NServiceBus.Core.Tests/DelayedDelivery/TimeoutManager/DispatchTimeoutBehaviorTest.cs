namespace NServiceBus.Core.Tests.Timeout.TimeoutManager
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;
    using Transport;

    public class DispatchTimeoutBehaviorTest
    {
        [Test]
        public async Task Invoke_when_message_dispatched_should_remove_timeout_from_timeout_storage()
        {
            var messageDispatcher = new FakeMessageDispatcher();
            var timeoutPersister = new InMemoryTimeoutPersister(() => DateTime.UtcNow);
            var behavior = new DispatchTimeoutBehavior(messageDispatcher, timeoutPersister, TransportTransactionMode.TransactionScope);
            var timeoutData = CreateTimeout();
            await timeoutPersister.Add(timeoutData, null);

            await behavior.Invoke(CreateContext(timeoutData.Id));

            var result = await timeoutPersister.Peek(timeoutData.Id, null);
            Assert.Null(result);
        }

        [Test]
        public async Task Invoke_when_timeout_not_in_storage_should_process_successfully()
        {
            var messageDispatcher = new FakeMessageDispatcher();
            var timeoutPersister = new InMemoryTimeoutPersister(() => DateTime.UtcNow);
            var behavior = new DispatchTimeoutBehavior(messageDispatcher, timeoutPersister, TransportTransactionMode.TransactionScope);

            await behavior.Invoke(CreateContext(Guid.NewGuid().ToString()));

            Assert.AreEqual(0, messageDispatcher.OutgoingTransportOperations.UnicastTransportOperations.Count());
        }

        [Test]
        public async Task Invoke_when_dispatching_message_fails_should_keep_timeout_in_storage()
        {
            var messageDispatcher = new FailingMessageDispatcher();
            var timeoutPersister = new InMemoryTimeoutPersister(() => DateTime.UtcNow);
            var behavior = new DispatchTimeoutBehavior(messageDispatcher, timeoutPersister, TransportTransactionMode.TransactionScope);
            var timeoutData = CreateTimeout();
            await timeoutPersister.Add(timeoutData, null);

            Assert.That(async () => await behavior.Invoke(CreateContext(timeoutData.Id)), Throws.InstanceOf<Exception>());

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

            Assert.That(async () => await behavior.Invoke(CreateContext(Guid.NewGuid().ToString())), Throws.InstanceOf<Exception>());
        }

        [Test]
        public async Task Invoke_when_using_dtc_should_enlist_dispatch_in_transaction()
        {
            var messageDispatcher = new FakeMessageDispatcher();
            var timeoutPersister = new InMemoryTimeoutPersister(() => DateTime.UtcNow);
            var behavior = new DispatchTimeoutBehavior(messageDispatcher, timeoutPersister, TransportTransactionMode.TransactionScope);
            var timeoutData = CreateTimeout();
            await timeoutPersister.Add(timeoutData, null);

            await behavior.Invoke(CreateContext(timeoutData.Id));

            var transportOperation = messageDispatcher.OutgoingTransportOperations.UnicastTransportOperations.Single();
            Assert.AreEqual(DispatchConsistency.Default, transportOperation.RequiredDispatchConsistency);
        }

        [Test]
        public async Task Invoke_should_pass_transport_transaction_from_message_context()
        {
            var messageDispatcher = new FakeMessageDispatcher();
            var timeoutPersister = new InMemoryTimeoutPersister(() => DateTime.UtcNow);
            var timeoutData = CreateTimeout();
            await timeoutPersister.Add(timeoutData, null);
            var context = CreateContext(timeoutData.Id);

            var behavior = new DispatchTimeoutBehavior(messageDispatcher, timeoutPersister, TransportTransactionMode.TransactionScope);
            await behavior.Invoke(context);

            Assert.AreSame(context.TransportTransaction, messageDispatcher.TransportTransactionUsed, "Wrong transport transaction passed to the dispatcher");
        }

        [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
        [TestCase(TransportTransactionMode.ReceiveOnly)]
        [TestCase(TransportTransactionMode.None)]
        public async Task Invoke_when_not_using_dtc_transport_should_not_enlist_dispatch_in_transaction(TransportTransactionMode nonDtcTxSettings)
        {
            var messageDispatcher = new FakeMessageDispatcher();
            var timeoutPersister = new InMemoryTimeoutPersister(() => DateTime.UtcNow);
            var behavior = new DispatchTimeoutBehavior(messageDispatcher, timeoutPersister, nonDtcTxSettings);
            var timeoutData = CreateTimeout();
            await timeoutPersister.Add(timeoutData, null);

            await behavior.Invoke(CreateContext(timeoutData.Id));

            var transportOperation = messageDispatcher.OutgoingTransportOperations.UnicastTransportOperations.Single();
            Assert.AreEqual(DispatchConsistency.Isolated, transportOperation.RequiredDispatchConsistency);
        }

        static TimeoutData CreateTimeout()
        {
            return new TimeoutData
            {
                Destination = "endpointQueue",
                Headers = new Dictionary<string, string>()
            };
        }

        static MessageContext CreateContext(string timeoutId)
        {
            var messageId = Guid.NewGuid().ToString("D");
            var headers = new Dictionary<string, string>
            {
                {"Timeout.Id", timeoutId}
            };

            return new MessageContext(messageId, headers, new byte[0], new TransportTransaction(), new CancellationTokenSource(), new ContextBag());
        }

        class FakeMessageDispatcher : IDispatchMessages
        {
            public TransportOperations OutgoingTransportOperations { get; private set; } = new TransportOperations();
            public TransportTransaction TransportTransactionUsed { get; private set; }

            public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transportTransaction, CancellationToken cancellationToken)
            {
                OutgoingTransportOperations = outgoingMessages;
                TransportTransactionUsed = transportTransaction;
                return Task.CompletedTask;
            }
        }

        class FailingMessageDispatcher : IDispatchMessages
        {
            public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transportTransaction, CancellationToken cancellationToken)
            {
                throw new Exception("simulated exception");
            }
        }

        class FakeTimeoutStorage : IPersistTimeouts
        {
            public Func<string, ContextBag, bool> OnTryRemove { get; set; } = (id, bag) => true;
            public Func<string, ContextBag, TimeoutData> OnPeek { get; set; } = (id, bag) => null;

            public Task Add(TimeoutData timeout, ContextBag context)
            {
                return Task.CompletedTask;
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
                return Task.CompletedTask;
            }
        }
    }
}
