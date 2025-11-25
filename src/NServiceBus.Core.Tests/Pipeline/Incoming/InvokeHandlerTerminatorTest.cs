namespace NServiceBus.Core.Tests.Pipeline.Incoming;

using System;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using NServiceBus.Sagas;
using NUnit.Framework;
using OpenTelemetry;
using Testing;

[TestFixture]
public class InvokeHandlerTerminatorTest
{
    InvokeHandlerTerminator terminator = new(new IncomingPipelineMetrics(new TestMeterFactory(), "queue", "disc"));

    [Test]
    public async Task When_saga_found_and_handler_is_saga_should_invoke_handler()
    {
        var handlerInvoked = false;
        var saga = new FakeSaga();

        var messageHandler = CreateMessageHandler((i, m, ctx) => handlerInvoked = true, saga);
        var behaviorContext = CreateBehaviorContext(messageHandler);
        AssociateSagaWithMessage(saga, behaviorContext);

        await terminator.Invoke(behaviorContext, _ => Task.CompletedTask);

        Assert.That(handlerInvoked, Is.True);
    }

    [Test]
    public async Task When_saga_not_found_and_handler_is_saga_should_not_invoke_handler()
    {
        var handlerInvoked = false;
        var saga = new FakeSaga();

        var messageHandler = CreateMessageHandler((i, m, ctx) => handlerInvoked = true, saga);
        var behaviorContext = CreateBehaviorContext(messageHandler);
        var sagaInstance = AssociateSagaWithMessage(saga, behaviorContext);
        sagaInstance.MarkAsNotFound();

        await terminator.Invoke(behaviorContext, _ => Task.CompletedTask);

        Assert.That(handlerInvoked, Is.False);
    }

    [Test]
    public async Task When_saga_not_found_and_handler_is_not_saga_should_invoke_handler()
    {
        var handlerInvoked = false;

        var messageHandler = CreateMessageHandler((i, m, ctx) => handlerInvoked = true, new FakeMessageHandler());
        var behaviorContext = CreateBehaviorContext(messageHandler);
        var sagaInstance = AssociateSagaWithMessage(new FakeSaga(), behaviorContext);
        sagaInstance.MarkAsNotFound();

        await terminator.Invoke(behaviorContext, _ => Task.CompletedTask);

        Assert.That(handlerInvoked, Is.True);
    }

    [Test]
    public async Task When_no_saga_should_invoke_handler()
    {
        var handlerInvoked = false;

        var messageHandler = CreateMessageHandler((i, m, ctx) => handlerInvoked = true, new FakeMessageHandler());
        var behaviorContext = CreateBehaviorContext(messageHandler);

        await terminator.Invoke(behaviorContext, _ => Task.CompletedTask);

        Assert.That(handlerInvoked, Is.True);
    }

    [Test]
    public async Task Should_invoke_handler_with_current_message()
    {
        object receivedMessage = null;
        var messageHandler = CreateMessageHandler((i, m, ctx) => receivedMessage = m, new FakeMessageHandler());
        var behaviorContext = CreateBehaviorContext(messageHandler);

        await terminator.Invoke(behaviorContext, _ => Task.CompletedTask);

        Assert.That(receivedMessage, Is.SameAs(behaviorContext.MessageBeingHandled));
    }

    [Test]
    public void Should_rethrow_exception_with_additional_data()
    {
        var thrownException = new InvalidOperationException();
        var messageHandler = CreateMessageHandler((i, m, ctx) => throw thrownException, new FakeMessageHandler());
        var behaviorContext = CreateBehaviorContext(messageHandler);

        var caughtException = Assert.ThrowsAsync<InvalidOperationException>(async () => await terminator.Invoke(behaviorContext, _ => Task.CompletedTask));

        Assert.That(caughtException, Is.SameAs(thrownException));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(caughtException.Data["Message type"], Is.EqualTo("System.Object"));
            Assert.That(caughtException.Data["Handler type"], Is.EqualTo("NServiceBus.Core.Tests.Pipeline.Incoming.InvokeHandlerTerminatorTest+FakeMessageHandler"));
            Assert.That(DateTimeOffsetHelper.ToDateTimeOffset((string)caughtException.Data["Handler start time"]), Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromSeconds(5)));
            Assert.That(DateTimeOffsetHelper.ToDateTimeOffset((string)caughtException.Data["Handler failure time"]), Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromSeconds(5)));
        }
    }

    [Test]
    public void Should_throw_friendly_exception_if_handler_returns_null()
    {
        var messageHandler = CreateMessageHandlerThatReturnsNull((i, m, ctx) => { }, new FakeSaga());
        var behaviorContext = CreateBehaviorContext(messageHandler);

        Assert.That(async () => await terminator.Invoke(behaviorContext, _ => Task.CompletedTask), Throws.Exception.With.Message.EqualTo("Return a Task or mark the method as async."));
    }

    static ActiveSagaInstance AssociateSagaWithMessage(FakeSaga saga, TestableInvokeHandlerContext behaviorContext)
    {
        var sagaInstance = new ActiveSagaInstance(saga, SagaMetadata.Create(typeof(FakeSaga)), () => DateTime.UtcNow);
        behaviorContext.Extensions.Set(sagaInstance);
        return sagaInstance;
    }

    static TestableHandlerContext CreateMessageHandler(Action<object, object, IMessageHandlerContext> invocationAction, object handlerInstance)
    {
        var messageHandler = new TestableHandlerContext((instance, message, handlerContext) =>
        {
            invocationAction(instance, message, handlerContext);
            return Task.CompletedTask;
        }, handlerInstance)
        {
            HandlerType = handlerInstance.GetType()
        };
        return messageHandler;
    }

    static TestableHandlerContext CreateMessageHandlerThatReturnsNull(Action<object, object, IMessageHandlerContext> invocationAction, object handlerInstance)
    {
        var messageHandler = new TestableHandlerContext((instance, message, handlerContext) =>
        {
            invocationAction(instance, message, handlerContext);
            return null;
        }, handlerInstance)
        {
            HandlerType = handlerInstance.GetType()
        };
        return messageHandler;
    }

    static TestableInvokeHandlerContext CreateBehaviorContext(MessageHandler messageHandler)
    {
        var behaviorContext = new TestableInvokeHandlerContext { MessageHandler = messageHandler };

        return behaviorContext;
    }

    class TestableHandlerContext(Func<object, object, IMessageHandlerContext, Task> invocationAction, object handlerInstance) : MessageHandler
    {
        public override object Instance { get; set; } = handlerInstance;
        public override Task Invoke(object message, IMessageHandlerContext handlerContext) => invocationAction(Instance, message, handlerContext);
    }

    class FakeSaga : Saga<FakeSaga.FakeSagaData>, IAmStartedByMessages<StartMessage>
    {
        public Task Handle(StartMessage message, IMessageHandlerContext context) => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<FakeSagaData> mapper) => mapper.MapSaga(s => s.SomeId).ToMessage<StartMessage>(msg => msg.SomeId);

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