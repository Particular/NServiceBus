namespace NServiceBus.Core.Tests.Sagas
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using System.Threading.Tasks;
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

            incomingContext.Builder.Register<IHandleSagaNotFound>(new HandleSagaNotFoundReturnsNull1(), validSagaHandler);

            Assert.That(async () => await behavior.Invoke(incomingContext, ctx => TaskEx.CompletedTask), Throws.Nothing);

            Assert.False(validSagaHandler.Handled);
        }

        [Test]
        public void Throw_friendly_exception_when_any_IHandleSagaNotFound_Handler_returns_null()
        {
            incomingContext.Builder.Register<IHandleSagaNotFound>(new HandleSagaNotFoundReturnsNull1(), new HandleSagaNotFoundValid());

            Assert.That(async () => await behavior.Invoke(incomingContext, SetSagaNotFound), Throws.Exception.With.Message.EqualTo("Return a Task or mark the method as async."));
        }


        static Task SetSagaNotFound(IIncomingLogicalMessageContext context)
        {
            context.Extensions.Get<SagaInvocationResult>().SagaNotFound(SagaMetadata.Create(typeof(DummySagaToProvideMetadata)));
            return Task.CompletedTask;
        }

        public class DummySagaToProvideMetadata : Saga<DummySagaToProvideMetadata.SagaData>, IHandleMessages<StartSaga>, IAmStartedByMessages<MessageToSaga>
        {
            public Task Handle(MessageToSaga message, IMessageHandlerContext context)
            {
                Data.MessageId = message.Id;
                return Task.FromResult(0);
            }

            public Task Handle(StartSaga message, IMessageHandlerContext context)
            {
                Data.MessageId = message.Id;
                return Task.FromResult(0);
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<StartSaga>(m => m.Id).ToSaga(s => s.MessageId);
                mapper.ConfigureMapping<MessageToSaga>(m => m.Id).ToSaga(s => s.MessageId);
            }

            public class SagaData : ContainSagaData
            {
                public virtual Guid MessageId { get; set; }
            }
        }

        public class StartSaga : ICommand
        {
            public Guid Id { get; set; }
        }

        public class MessageToSaga : ICommand
        {
            public Guid Id { get; set; }
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

        InvokeSagaNotFoundBehavior behavior;
        TestableIncomingLogicalMessageContext incomingContext;
    }
}
