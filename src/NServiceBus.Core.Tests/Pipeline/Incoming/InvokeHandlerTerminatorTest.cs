namespace NServiceBus.Core.Tests.Pipeline.Incoming
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class InvokeHandlerTerminatorTest
    {
        [Test]
        public async Task When_saga_found_and_handler_is_saga_should_invoke_handler()
        {
            var handlerInvoked = false;
            var terminator = new InvokeHandlerTerminator();
            var saga = new FakeSaga();

            var messageHandler = CreateMessageHandler((i, m, ctx) => handlerInvoked = true, saga);
            var behaviorContext = CreateBehaviorContext(messageHandler);
            AssociateSagaWithMessage(saga, behaviorContext);

            await terminator.Invoke(behaviorContext, _ => Task.CompletedTask);

            Assert.IsTrue(handlerInvoked);
        }

        [Test]
        public async Task When_saga_not_found_and_handler_is_saga_should_not_invoke_handler()
        {
            var handlerInvoked = false;
            var terminator = new InvokeHandlerTerminator();
            var saga = new FakeSaga();

            var messageHandler = CreateMessageHandler((i, m, ctx) => handlerInvoked = true, saga);
            var behaviorContext = CreateBehaviorContext(messageHandler);
            var sagaInstance = AssociateSagaWithMessage(saga, behaviorContext);
            sagaInstance.MarkAsNotFound();

            await terminator.Invoke(behaviorContext, _ => Task.CompletedTask);

            Assert.IsFalse(handlerInvoked);
        }

        [Test]
        public async Task When_saga_not_found_and_handler_is_not_saga_should_invoke_handler()
        {
            var handlerInvoked = false;
            var terminator = new InvokeHandlerTerminator();

            var messageHandler = CreateMessageHandler((i, m, ctx) => handlerInvoked = true, new FakeMessageHandler());
            var behaviorContext = CreateBehaviorContext(messageHandler);
            var sagaInstance = AssociateSagaWithMessage(new FakeSaga(), behaviorContext);
            sagaInstance.MarkAsNotFound();

            await terminator.Invoke(behaviorContext, _ => Task.CompletedTask);

            Assert.IsTrue(handlerInvoked);
        }

        [Test]
        public async Task When_no_saga_should_invoke_handler()
        {
            var handlerInvoked = false;
            var terminator = new InvokeHandlerTerminator();

            var messageHandler = CreateMessageHandler((i, m, ctx) => handlerInvoked = true, new FakeMessageHandler());
            var behaviorContext = CreateBehaviorContext(messageHandler);

            await terminator.Invoke(behaviorContext, _ => Task.CompletedTask);

            Assert.IsTrue(handlerInvoked);
        }

        [Test]
        public async Task Should_invoke_handler_with_current_message()
        {
            object receivedMessage = null;
            var terminator = new InvokeHandlerTerminator();
            var messageHandler = CreateMessageHandler((i, m, ctx) => receivedMessage = m, new FakeMessageHandler());
            var behaviorContext = CreateBehaviorContext(messageHandler);

            await terminator.Invoke(behaviorContext, _ => Task.CompletedTask);

            Assert.AreSame(behaviorContext.MessageBeingHandled, receivedMessage);
        }

        [Test]
        public void Should_rethrow_exception_with_additional_data()
        {
            var thrownException = new InvalidOperationException();
            var terminator = new InvokeHandlerTerminator();
            var messageHandler = CreateMessageHandler((i, m, ctx) => throw thrownException, new FakeMessageHandler());
            var behaviorContext = CreateBehaviorContext(messageHandler);

            var caughtException = Assert.ThrowsAsync<InvalidOperationException>(async () => await terminator.Invoke(behaviorContext, _ => Task.CompletedTask));

            Assert.AreSame(thrownException, caughtException);
            Assert.AreEqual("System.Object", caughtException.Data["Message type"]);
            Assert.AreEqual("NServiceBus.Core.Tests.Pipeline.Incoming.InvokeHandlerTerminatorTest+FakeMessageHandler", caughtException.Data["Handler type"]);
            Assert.That(DateTimeOffsetHelper.ToDateTimeOffset((string)caughtException.Data["Handler start time"]), Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromSeconds(5)));
            Assert.That(DateTimeOffsetHelper.ToDateTimeOffset((string)caughtException.Data["Handler failure time"]), Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromSeconds(5)));
        }

        [Test]
        public void Should_throw_friendly_exception_if_handler_returns_null()
        {
            var terminator = new InvokeHandlerTerminator();
            var messageHandler = CreateMessageHandlerThatReturnsNull((i, m, ctx) => { }, new FakeSaga());
            var behaviorContext = CreateBehaviorContext(messageHandler);

            Assert.That(async () => await terminator.Invoke(behaviorContext, _ => Task.CompletedTask), Throws.Exception.With.Message.EqualTo("Return a Task or mark the method as async."));
        }

        static ActiveSagaInstance AssociateSagaWithMessage(FakeSaga saga, IInvokeHandlerContext behaviorContext)
        {
            var sagaInstance = new ActiveSagaInstance(saga, SagaMetadata.Create(typeof(FakeSaga), new List<Type>(), new Conventions()), () => DateTime.UtcNow);
            behaviorContext.Extensions.Set(sagaInstance);
            return sagaInstance;
        }

        static MessageHandler CreateMessageHandler(Action<object, object, IMessageHandlerContext> invocationAction, object handlerInstance)
        {
            var messageHandler = new MessageHandler((instance, message, handlerContext) =>
            {
                invocationAction(instance, message, handlerContext);
                return Task.CompletedTask;
            }, handlerInstance.GetType())
            {
                Instance = handlerInstance
            };
            return messageHandler;
        }

        static MessageHandler CreateMessageHandlerThatReturnsNull(Action<object, object, IMessageHandlerContext> invocationAction, object handlerInstance)
        {
            var messageHandler = new MessageHandler((instance, message, handlerContext) =>
            {
                invocationAction(instance, message, handlerContext);
                return null;
            }, handlerInstance.GetType())
            {
                Instance = handlerInstance
            };
            return messageHandler;
        }

        static IInvokeHandlerContext CreateBehaviorContext(MessageHandler messageHandler)
        {
            var behaviorContext = new TestableInvokeHandlerContext
            {
                MessageHandler = messageHandler
            };

            return behaviorContext;
        }

        class FakeSaga : Saga<FakeSaga.FakeSagaData>, IAmStartedByMessages<StartMessage>
        {
            public Task Handle(StartMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<FakeSagaData> mapper)
            {
                mapper.ConfigureMapping<StartMessage>(msg => msg.SomeId).ToSaga(saga => saga.SomeId);
            }

            public class FakeSagaData : ContainSagaData
            {
                public string SomeId { get; set; }
            }
        }

        class StartMessage
        {
            public string SomeId { get; set; }
        }

        class FakeMessageHandler
        {
        }
    }
}