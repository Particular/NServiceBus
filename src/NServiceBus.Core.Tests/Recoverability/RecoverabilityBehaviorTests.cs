namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class RecoverabilityBehaviorTests
    {
        static string ErrorQueueAddress = "errors";
        ITransportReceiveContext context;

        [SetUp]
        public void SetUp()
        {
            context = new TestableTransportReceiveContext
            {
                Message = new IncomingMessage("message-id", new Dictionary<string, string>(), Stream.Null)
            };

            context.Extensions.Set<IEventAggregator>(new FakeEventAggregator());
        }

        [Test]
        public async Task ShouldMoveFailedMessageToErrorQueueImmediately_WhenRunningWithNoTransactions()
        {
            var dispatcher = new FakeDispatcher();
            var behavior = CreateBehavior(dispatcher);

            await behavior.Invoke(context, () => { throw new Exception(); });

            Assert.IsNotNull(dispatcher.TransportOperation.Message);
        }

        [Test]
        public async Task ShouldNotMoveFailedMessageToErrorQueueImmediately_WhenRunningWithTransactions()
        {
            var dispatcher = new FakeDispatcher();
            var behavior = CreateBehavior(dispatcher, transactionsEnabled: true);

            await behavior.Invoke(context, () => { throw new Exception(); });

            Assert.IsNull(dispatcher.TransportOperation);
        }

        [Test]
        public async Task ShouldMoveFailedMessageToErrorQueueOnSecondInvocation_WhenRunningWithTransactions()
        {
            var dispatcher = new FakeDispatcher();
            var behavior = CreateBehavior(dispatcher, transactionsEnabled: true);

            await behavior.Invoke(context, () => { throw new Exception(); });

            await behavior.Invoke(context, () => Task.FromResult(0));

            Assert.IsNotNull(dispatcher.TransportOperation.Message);
        }

        [Test]
        public async Task ShouldNotCallRestOfThePipelineWhenMovingMesasgeToErrorQueue_WhenRunningWithTransactions()
        {
            var dispatcher = new FakeDispatcher();
            var behavior = CreateBehavior(dispatcher, transactionsEnabled: true);
            var pipelineCalled = false;

            await behavior.Invoke(context, () => { throw new Exception(); });

            await behavior.Invoke(context, () => {
                pipelineCalled = true;
                return Task.FromResult(0);
            });

            Assert.IsFalse(pipelineCalled);
        }

        [Test]
        public async Task ShouldSkipRetryForDeserializationErrors()
        {
            var dispatcher = new FakeDispatcher();
            var behavior = CreateBehavior(dispatcher, flrEnabled: true, slrEnabled: true, transactionsEnabled: true);

            await behavior.Invoke(context, () => { throw new MessageDeserializationException(string.Empty); });

            await behavior.Invoke(context, () => Task.FromResult(0));

            Assert.IsNotNull(dispatcher.TransportOperation);
            Assert.AreEqual(ErrorQueueAddress, dispatcher.TransportOperation.Destination);
        }

        RecoverabilityBehavior CreateBehavior(IDispatchMessages messageDispatcher, bool flrEnabled = false, bool slrEnabled = false, bool transactionsEnabled = false)
        {
            var failureStorage = new FailureInfoStorage(1000);

            var flrHandler = flrEnabled ? new FirstLevelRetriesHandler(failureStorage, new FirstLevelRetryPolicy(2)) : null;

            var slrHandler = slrEnabled 
                ? new SecondLevelRetriesHandler(new DefaultSecondLevelRetryPolicy(2, TimeSpan.FromSeconds(3)), failureStorage, new DelayedRetryExecutor("input", messageDispatcher))
                : null; 

            var errorHandler = new MoveFaultsToErrorQueueHandler(
                new FakeCriticalError(),
                failureStorage,
                new MoveToErrorsActionExecutor(messageDispatcher, ErrorQueueAddress, new Dictionary<string, string>()));

            return new RecoverabilityBehavior(flrHandler, slrHandler, errorHandler, transactionsEnabled);
        }

        class FakeDispatcher : IDispatchMessages
        {
            public Task Dispatch(TransportOperations outgoingMessages, ContextBag context)
            {
                TransportOperation = outgoingMessages.UnicastTransportOperations.First();

                return Task.FromResult(0);
            }

            public UnicastTransportOperation TransportOperation { get; private set; }
        }

        class FakeCriticalError : CriticalError
        {
            public FakeCriticalError() : base(_ => TaskEx.CompletedTask)
            {
            }

            public override void Raise(string errorMessage, Exception exception)
            {
            }
        }
    }
}