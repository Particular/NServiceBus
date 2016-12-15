﻿namespace NServiceBus.Core.Tests.Handlers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Unicast;

    [TestFixture]
    public class MessageHandlerRegistryTests
    {
        [TestCase(typeof(HandlerWithIMessageSessionProperty))]
        [TestCase(typeof(HandlerWithIEndpointInstanceProperty))]
        [TestCase(typeof(HandlerWithIMessageSessionCtorDep))]
        [TestCase(typeof(HandlerWithIEndpointInstanceCtorDep))]
        [TestCase(typeof(HandlerWithInheritedIMessageSessionPropertyDep))]
        [TestCase(typeof(SagaWithIllegalDep))]
        public void ShouldThrowIfUserTriesToBypassTheHandlerContext(Type handlerType)
        {
            var registry = new MessageHandlerRegistry(new Conventions());

            Assert.Throws<Exception>(() => registry.RegisterHandler(handlerType));
        }

        [Test]        public void ShouldIndicateWhetherAHandlerIsATimeoutHandler()        {            var registry = new MessageHandlerRegistry(new Conventions());            registry.RegisterHandler(typeof(SagaWithTimeoutOfMessage));            var handlers = registry.GetHandlersFor(typeof(MyMessage));

            Assert.AreEqual(2, handlers.Count);

            var timeoutHandler = handlers.SingleOrDefault(h => h.IsTimeoutHandler);

            Assert.NotNull(timeoutHandler, "Timeout handler should be marked as such");

            var timeoutInstance = new SagaWithTimeoutOfMessage();
            timeoutHandler.Instance = timeoutInstance;

            timeoutHandler.Invoke(null, null);

            Assert.True(timeoutInstance.TimeoutCalled);
            Assert.False(timeoutInstance.HandlerCalled);

            var regularHandler = handlers.SingleOrDefault(h => !h.IsTimeoutHandler);

            Assert.NotNull(regularHandler, "Regular handler should be marked as timeout handler");

            var regularInstance = new SagaWithTimeoutOfMessage();
            regularHandler.Instance = regularInstance;

            regularHandler.Invoke(null, null);

            Assert.False(regularInstance.TimeoutCalled);
            Assert.True(regularInstance.HandlerCalled);
        }

        class HandlerWithIMessageSessionProperty : IHandleMessages<MyMessage>
        {
            public IMessageSession MessageSession { get; set; }

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }
        }

        class HandlerWithIEndpointInstanceProperty : IHandleMessages<MyMessage>
        {
            public IEndpointInstance EndpointInstance { get; set; }

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }
        }

        class HandlerWithIMessageSessionCtorDep : IHandleMessages<MyMessage>
        {
            public HandlerWithIMessageSessionCtorDep(IMessageSession messageSession)
            {
                MessageSession = messageSession;
            }

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            IMessageSession MessageSession;
        }

        class HandlerWithInheritedIMessageSessionPropertyDep : HandlerBaseWithIMessageSessionDep, IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }
        }

        class HandlerWithIEndpointInstanceCtorDep : IHandleMessages<MyMessage>
        {
            public HandlerWithIEndpointInstanceCtorDep(IEndpointInstance endpointInstance)
            {
                this.endpointInstance = endpointInstance;
            }

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            IEndpointInstance endpointInstance;
        }

        class SagaWithIllegalDep : Saga<SagaWithIllegalDep.MySagaData>, IAmStartedByMessages<MyMessage>
        {
            public SagaWithIllegalDep(IEndpointInstance endpointInstance)
            {
                this.endpointInstance = endpointInstance;
            }

            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper)
            {
                throw new NotImplementedException();
            }

            IEndpointInstance endpointInstance;

            public class MySagaData : ContainSagaData
            {
            }
        }

        class HandlerBaseWithIMessageSessionDep
        {
            public IMessageSession MessageSession { get; set; }
        }

        class MyMessage : IMessage
        {
        }

        class SagaWithTimeoutOfMessage : Saga<SagaWithTimeoutOfMessage.MySagaData>, IAmStartedByMessages<MyMessage>, IHandleTimeouts<MyMessage>        {            public Task Handle(MyMessage message, IMessageHandlerContext context)            {
                HandlerCalled = true;
                return TaskEx.CompletedTask;
            }            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper)            {                throw new NotImplementedException();            }            public Task Timeout(MyMessage state, IMessageHandlerContext context)
            {
                TimeoutCalled = true;
                return TaskEx.CompletedTask;
            }

            public bool HandlerCalled { get; set; }
            public bool TimeoutCalled { get; set; }            public class MySagaData : ContainSagaData            {            }        }

    }
}