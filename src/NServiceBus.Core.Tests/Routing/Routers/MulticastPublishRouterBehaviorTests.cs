namespace NServiceBus.Core.Tests.Routing.Routers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class MulticastPublishRouterBehaviorTests
    {
        [Test]
        public async Task Should_set_messageintent_to_publish()
        {
            var router = new MulticastPublishRouterBehavior();
            var context = new FakeContext();

            await router.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.AreEqual(1, context.Headers.Count);
            Assert.AreEqual(MessageIntentEnum.Publish.ToString(), context.Headers[Headers.MessageIntent]);
        }

        class FakeContext : IOutgoingPublishContext
        {
            public FakeContext()
            {
                Headers = new Dictionary<string, string>();
                Message = new OutgoingLogicalMessage(typeof(object), new object());
            }

            public ContextBag Extensions { get; }
            public IBuilder Builder { get; }

            public Task Send(object message, SendOptions options)
            {
                return null;
            }

            public Task Send<T>(Action<T> messageConstructor, SendOptions options)
            {
                return null;
            }

            public Task Publish(object message, PublishOptions options)
            {
                return null;
            }

            public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
            {
                return null;
            }

            public Task Subscribe(Type eventType, SubscribeOptions options)
            {
                return null;
            }

            public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
            {
                return null;
            }

            public string MessageId { get; }
            public Dictionary<string, string> Headers { get; }
            public OutgoingLogicalMessage Message { get; }
        }

        class Router : IUnicastRouter
        {
            public Task<IEnumerable<UnicastRoutingStrategy>> Route(Type messageType, DistributionStrategy distributionStrategy, ContextBag contextBag)
            {
                IEnumerable<UnicastRoutingStrategy> unicastRoutingStrategies = new List<UnicastRoutingStrategy>
                {
                    new UnicastRoutingStrategy("Fake")
                };
                return Task.FromResult(unicastRoutingStrategies);
            }
        }
    }
}