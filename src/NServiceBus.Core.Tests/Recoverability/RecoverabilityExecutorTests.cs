namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Transport;

    [TestFixture]
    public class RecoverabilityExecutorTests
    {
        [SetUp]
        public void SetUp()
        {
            dispatchCollector = new DispatchCollector();
        }

        [Test]
        public async Task When_unsupported_action_returned_should_move_to_errors()
        {
            var recoverabilityExecutor = CreateExecutor();
            var recoverabilityContext = CreateRecoverabilityContext(new UnsupportedAction(), messageId: "message-id");

            await recoverabilityExecutor.Invoke(recoverabilityContext);

            Assert.True(dispatchCollector.MessageWasSentTo(ErrorQueueAddress));
        }

        [Test]
        public async Task When_discard_action_returned_should_discard_message()
        {
            var recoverabilityExecutor = CreateExecutor();
            var recoverabilityContext = CreateRecoverabilityContext(new Discard("not needed anymore"), messageId: "message-id");

            await recoverabilityExecutor.Invoke(recoverabilityContext);

            Assert.True(dispatchCollector.NoMessageWasSent());
        }

        IRecoverabilityContext CreateRecoverabilityContext(
            RecoverabilityAction recoverabilityAction,
            Exception raisedException = null,
            string exceptionMessage = "default-message",
            string messageId = "default-id",
            int numberOfDeliveryAttempts = 1)
        {
            var errorContext = new ErrorContext(raisedException ?? new Exception(exceptionMessage), new Dictionary<string, string>(), messageId, new byte[0], new TransportTransaction(), numberOfDeliveryAttempts, "my-endpoint", new ContextBag());
            return new RecoverabilityContext(errorContext, null, recoverabilityAction, new FakeRootContext(dispatchCollector));
        }

        RecoverabilityExecutor CreateExecutor()
        {
            return new RecoverabilityExecutor(
                new DelayedRetryExecutor(),
                new MoveToErrorsExecutor(new Dictionary<string, string>(), headers => { }));
        }

        DispatchCollector dispatchCollector;

        static string ErrorQueueAddress = "error-queue";

        class UnsupportedAction : RecoverabilityAction
        {
            public override ErrorHandleResult ErrorHandleResult => throw new NotImplementedException();
        }

        class DispatchCollector
        {
            string targetAddress;

            public IDictionary<string, string> MessageHeaders { get; private set; }

            public void Collect(TransportOperation transportOperation)
            {
                var unicastAddressTag = transportOperation.AddressTag as UnicastAddressTag;

                Assert.IsNotNull(unicastAddressTag);

                targetAddress = unicastAddressTag.Destination;

                MessageHeaders = transportOperation.Message.Headers;
            }

            public bool MessageWasSentTo(string address)
            {
                return address == targetAddress;
            }

            public bool NoMessageWasSent()
            {
                return targetAddress == null;
            }
        }

        class FakeRootContext : IBehaviorContext
        {
            public FakeRootContext(DispatchCollector dispatchCollector)
            {
                Extensions = new ContextBag();

                Extensions.Set<IPipelineCache>(new FakePipelineCache(dispatchCollector));
            }

            public IServiceProvider Builder => throw new NotImplementedException();

            public CancellationToken CancellationToken => CancellationToken.None;

            public ContextBag Extensions { get; }
        }

        class FakePipelineCache : IPipelineCache
        {
            public FakePipelineCache(DispatchCollector dispatchCollector)
            {
                this.dispatchCollector = dispatchCollector;
            }

            public IPipeline<TContext> Pipeline<TContext>() where TContext : IBehaviorContext
            {
                return (IPipeline<TContext>)new FakeDispatchPipeline(dispatchCollector);
            }

            readonly DispatchCollector dispatchCollector;
        }

        class FakeDispatchPipeline : IPipeline<IDispatchContext>
        {
            public FakeDispatchPipeline(DispatchCollector dispatchCollector)
            {
                this.dispatchCollector = dispatchCollector;
            }

            public Task Invoke(IDispatchContext context)
            {
                dispatchCollector.Collect(context.Operations.Single());

                return Task.CompletedTask;
            }

            DispatchCollector dispatchCollector;
        }
    }
}