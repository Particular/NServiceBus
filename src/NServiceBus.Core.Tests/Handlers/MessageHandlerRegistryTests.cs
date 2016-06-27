namespace NServiceBus.Core.Tests.Handlers
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Unicast;

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
            public IEndpointInstance EndpointInstance { get; set; }
        }

        class MyMessage
        {
        }
    }
}