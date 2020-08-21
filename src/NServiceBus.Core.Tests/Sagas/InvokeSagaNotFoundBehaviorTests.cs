namespace NServiceBus.Core.Tests.Sagas
{
    using NServiceBus.Pipeline;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Testing;

    [TestFixture]
    public class InvokeSagaNotFoundBehaviorTests
    {
        [SetUp]
        public void SetupTests()
        {
            behavior = new InvokeSagaNotFoundBehavior();

            incomingContext = new TestableIncomingLogicalMessageContext();
        }

        [Test]
        public void SagaNotFound_handlers_are_not_called_when_saga_is_found()
        {
            var validSagaHandler = new HandleSagaNotFoundValid();

            incomingContext.Services.AddSingleton<IHandleSagaNotFound>(new HandleSagaNotFoundReturnsNull1());
            incomingContext.Services.AddSingleton<IHandleSagaNotFound>(validSagaHandler);

            Assert.That(async () => await behavior.Invoke(incomingContext, ctx => Task.CompletedTask), Throws.Nothing);

            Assert.False(validSagaHandler.Handled);
        }

        [Test]
        public void Throw_friendly_exception_when_any_IHandleSagaNotFound_Handler_returns_null()
        {
            incomingContext.Services.AddSingleton<IHandleSagaNotFound>(new HandleSagaNotFoundReturnsNull1());
            incomingContext.Services.AddSingleton<IHandleSagaNotFound>(new HandleSagaNotFoundValid());

            Assert.That(async () => await behavior.Invoke(incomingContext, SetSagaNotFound), Throws.Exception.With.Message.EqualTo("Return a Task or mark the method as async."));
        }


        static Task SetSagaNotFound(IIncomingLogicalMessageContext context)
        {
            context.Extensions.Get<SagaInvocationResult>().SagaNotFound();
            return Task.CompletedTask;
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

                return Task.CompletedTask;
            }
        }

        InvokeSagaNotFoundBehavior behavior;
        TestableIncomingLogicalMessageContext incomingContext;
    }
}
