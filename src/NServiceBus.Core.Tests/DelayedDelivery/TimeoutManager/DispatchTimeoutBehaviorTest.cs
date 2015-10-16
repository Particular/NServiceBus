namespace NServiceBus.Core.Tests.Timeout.TimeoutManager
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.InMemory.TimeoutPersister;
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
            var testee = new DispatchTimeoutBehavior(messageDispatcher, timeoutPersister, TransactionSupport.Distributed);
            var timeoutData = CreateTimeout();
            await timeoutPersister.Add(timeoutData, null);

            await testee.Invoke(CreateContext(timeoutData.Id), context => TaskEx.Completed);

            var result = await timeoutPersister.Peek(timeoutData.Id, null);
            Assert.Null(result);
        }

        [Test]
        public async Task Invoke_when_dispatching_message_fails_should_keep_timeout_in_storage()
        {
            var messageDispatcher = new FailingMessageDispatcher();
            var timeoutPersister = new InMemoryTimeoutPersister();
            var testee = new DispatchTimeoutBehavior(messageDispatcher, timeoutPersister, TransactionSupport.Distributed);
            var timeoutData = CreateTimeout();
            await timeoutPersister.Add(timeoutData, null);

            Assert.Throws<Exception>(async () => await testee.Invoke(CreateContext(timeoutData.Id), context => TaskEx.Completed));

            var result = await timeoutPersister.Peek(timeoutData.Id, null);
            Assert.NotNull(result);
        }

        [Test]
        public async Task Invoke_when_using_dtc_should_enlist_dispatch_in_transaction()
        {
            var messageDispatcher = new FakeMessageDispatcher();
            var timeoutPersister = new InMemoryTimeoutPersister();
            var testee = new DispatchTimeoutBehavior(messageDispatcher, timeoutPersister, TransactionSupport.Distributed);
            var timeoutData = CreateTimeout();
            await timeoutPersister.Add(timeoutData, null);

            await testee.Invoke(CreateContext(timeoutData.Id), context => TaskEx.Completed);

            var transportOperation = messageDispatcher.OutgoingMessages.Single();
            Assert.AreEqual(DispatchConsistency.Default, transportOperation.DispatchOptions.RequiredDispatchConsistency);
        }

        [TestCase(TransactionSupport.MultiQueue)]
        [TestCase(TransactionSupport.SingleQueue)]
        [TestCase(TransactionSupport.None)]
        public async Task Invoke_when_not_using_dtc_transport_should_not_enlist_dispatch_in_transaction(TransactionSupport nonDtcTxSettings)
        {
            var messageDispatcher = new FakeMessageDispatcher();
            var timeoutPersister = new InMemoryTimeoutPersister();
            var testee = new DispatchTimeoutBehavior(messageDispatcher, timeoutPersister, nonDtcTxSettings);
            var timeoutData = CreateTimeout();
            await timeoutPersister.Add(timeoutData, null);

            await testee.Invoke(CreateContext(timeoutData.Id), context => TaskEx.Completed);

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

        static PhysicalMessageProcessingContext CreateContext(string timeoutId)
        {
            var messageId = Guid.NewGuid().ToString("D");
            var headers = new Dictionary<string, string>
            {
                {"Timeout.Id", timeoutId}
            };

            return new PhysicalMessageProcessingContext(
                new IncomingMessage(messageId, headers, new MemoryStream()), null);
        }

        class FakeMessageDispatcher : IDispatchMessages
        {
            public IEnumerable<TransportOperation> OutgoingMessages { get; private set; }

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
    }
}