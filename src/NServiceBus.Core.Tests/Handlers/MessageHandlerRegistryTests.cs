namespace NServiceBus.Core.Tests.Handlers;

using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Testing;
using Unicast;

[TestFixture]
public class MessageHandlerRegistryTests
{
    [Test]
    public async Task ShouldIndicateWhetherAHandlerIsATimeoutHandler()
    {
        var registry = new MessageHandlerRegistry();

        registry.AddHandler<SagaWithTimeoutOfMessage>();

        var handlers = registry.GetHandlersFor(typeof(MyMessage));

        Assert.That(handlers.Count, Is.EqualTo(2));

        var timeoutHandler = handlers.SingleOrDefault(h => h.IsTimeoutHandler);

        Assert.That(timeoutHandler, Is.Not.Null, "Timeout handler should be marked as such");

        var timeoutInstance = new SagaWithTimeoutOfMessage();

        timeoutHandler.Instance = timeoutInstance;
        await timeoutHandler.Invoke(new MyMessage(), new TestableInvokeHandlerContext());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(timeoutInstance.TimeoutCalled, Is.True);
            Assert.That(timeoutInstance.HandlerCalled, Is.False);
        }

        var regularHandler = handlers.SingleOrDefault(h => !h.IsTimeoutHandler);

        Assert.That(regularHandler, Is.Not.Null, "Regular handler should be marked as timeout handler");

        var regularInstance = new SagaWithTimeoutOfMessage();

        regularHandler.Instance = regularInstance;
        await regularHandler.Invoke(new MyMessage(), new TestableInvokeHandlerContext());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(regularInstance.TimeoutCalled, Is.False);
            Assert.That(regularInstance.HandlerCalled, Is.True);
        }
    }

    [Test]
    public void ShouldRegisterMultipleHandlerInterfaces()
    {
        var registry = new MessageHandlerRegistry();
        registry.AddHandler<HandlerForMultipleMessages>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(registry.GetHandlersFor(typeof(MyMessage)), Has.Count.EqualTo(1));
            Assert.That(registry.GetHandlersFor(typeof(AnotherMessage)), Has.Count.EqualTo(1));
        }
    }

    [Test]
    public void ShouldDeduplicate()
    {
        var registry = new MessageHandlerRegistry();
        registry.AddHandler<HandlerForMultipleMessages>();
        registry.AddHandler<HandlerForMultipleMessages>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(registry.GetHandlersFor(typeof(MyMessage)), Has.Count.EqualTo(1));
            Assert.That(registry.GetHandlersFor(typeof(AnotherMessage)), Has.Count.EqualTo(1));
        }
    }

    class HandlerForMultipleMessages : IHandleMessages<MyMessage>, IHandleMessages<AnotherMessage>
    {
        public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;

        public Task Handle(AnotherMessage message, IMessageHandlerContext context) => Task.CompletedTask;
    }

    class MyMessage : IMessage;

    class AnotherMessage : IMessage;

    class SagaWithTimeoutOfMessage : Saga<SagaWithTimeoutOfMessage.MySagaData>, IAmStartedByMessages<MyMessage>, IHandleTimeouts<MyMessage>
    {

        public Task Handle(MyMessage message, IMessageHandlerContext context)
        {
            HandlerCalled = true;
            return Task.CompletedTask;
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper) => throw new NotImplementedException();

        public Task Timeout(MyMessage state, IMessageHandlerContext context)
        {
            TimeoutCalled = true;
            return Task.CompletedTask;
        }

        public bool HandlerCalled { get; set; }
        public bool TimeoutCalled { get; set; }

        public class MySagaData : ContainSagaData;
    }

}