namespace NServiceBus.Core.Tests.Sagas
{
    using NServiceBus.Pipeline;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using ObjectBuilder;
    using Unicast.Messages;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    [TestFixture]
    public class InvokeSagaNotFoundBehaviorTests
    {
        [SetUp]
        public void SetupTests()
        {
            message = new LogicalMessage(new MessageMetadata(typeof(TestMessage)), new TestMessage());

            behavior = new InvokeSagaNotFoundBehavior();

            incomingContext = new IncomingLogicalMessageContext(
                message,
                "messageId",
                "replyToAddress",
                new Dictionary<string, string>(),
                null);

            builder = new FuncBuilder();

            incomingContext.Set<IBuilder>(builder);
        }

        [Test]
        public void SagaNotFound_handlers_are_not_called_when_saga_is_found()
        {
            var validSagaHandler = new HandleSagaNotFoundValid();

            builder.Register<IHandleSagaNotFound>(() => new HandleSagaNotFoundReturnsNull1());
            builder.Register<IHandleSagaNotFound>(() => validSagaHandler);

            Assert.That(async () => await behavior.Invoke(incomingContext, () => TaskEx.CompletedTask), Throws.Nothing);

            Assert.False(validSagaHandler.Handled);
        }

        [Test]
        public void Throw_friendly_exception_when_any_IHandleSagaNotFound_Handler_returns_null()
        {
            builder.Register<IHandleSagaNotFound>(() => new HandleSagaNotFoundReturnsNull1());
            builder.Register<IHandleSagaNotFound>(() => new HandleSagaNotFoundValid());

            Assert.That(async () => await behavior.Invoke(incomingContext, SetSagaNotFound), Throws.Exception.With.Message.EqualTo("Return a Task or mark the method as async."));
        }


        static Task SetSagaNotFound(IIncomingLogicalMessageContext context)
        {
            context.Extensions.Get<SagaInvocationResult>().SagaNotFound();
            return TaskEx.CompletedTask;
        }

        class TestMessage : IMessage
        { }

        class HandleSagaNotFoundReturnsNull1 : IHandleSagaNotFound
        {
            public Task Handle(object message, IMessageProcessingContext context)
            {
                return null;
            }
        }

        class HandleSagaNotFoundValid : IHandleSagaNotFound
        {
            public bool Handled { get; private set; }

            public Task Handle(object message, IMessageProcessingContext context)
            {
                Handled = true;

                return TaskEx.CompletedTask;
            }
        }

        LogicalMessage message;
        InvokeSagaNotFoundBehavior behavior;
        FuncBuilder builder;
        IncomingLogicalMessageContext incomingContext;
    }
}
