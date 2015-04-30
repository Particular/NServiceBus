namespace NServiceBus.Core.Tests.AutomaticSubscriptions
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.AutomaticSubscriptions;
    using NUnit.Framework;
    using Unicast;
    using Unicast.Routing;

    public class AutoSubscriptionContext
    {
        [SetUp]
        public void SetUp()
        {
            autoSubscriptionStrategy = new DefaultAutoSubscriptionStrategy
            {
                HandlerRegistry = new MessageHandlerRegistry(),
                MessageRouter = new StaticMessageRouter(KnownMessageTypes())
            };
        }

        protected virtual IEnumerable<Type> KnownMessageTypes()
        {
            return new List<Type>();
        }

        protected void RegisterMessageHandlerType<T>()
        {
            ((MessageHandlerRegistry)autoSubscriptionStrategy.HandlerRegistry).RegisterHandler(typeof(T));
        }

        protected void RegisterMessageType<T>(Address address)
        {
            ((StaticMessageRouter)autoSubscriptionStrategy.MessageRouter).RegisterMessageRoute(typeof(T), address);
        }

        protected DefaultAutoSubscriptionStrategy autoSubscriptionStrategy;

    }
}