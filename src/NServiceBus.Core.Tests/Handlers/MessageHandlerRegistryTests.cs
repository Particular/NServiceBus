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
        [TestCase(typeof(HandlerWithIEndpointInstanceCtorDep))]
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

        class MyMessage { }
    }

}