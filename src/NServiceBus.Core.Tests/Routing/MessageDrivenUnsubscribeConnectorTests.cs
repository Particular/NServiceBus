namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using Transport;
    using Unicast.Queuing;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class MessageDrivenUnsubscribeConnectorTests
    {
        MessageDrivenUnsubscribeConnector connector;
        FakeDispatcher dispatcher;
        Publishers publishers;

        [SetUp]
        public void SetUp()
        {
            publishers = new Publishers();
            publishers.AddOrReplacePublishers("A", new List<PublisherTableEntry> {new PublisherTableEntry(typeof(object), PublisherAddress.CreateFromPhysicalAddresses("publisher1"))});
            dispatcher = new FakeDispatcher();
            connector = new MessageDrivenUnsubscribeConnector(publishers, "replyToAddress", "Endpoint");
        }

        [Test]
        public async Task Should_Dispatch_for_all_publishers()
        {
            await connector.Invoke(new TestableUnsubscribeContext(), () => Task.FromResult(0), Dispatch);

            Assert.AreEqual(1, dispatcher.DispatchedTransportOperations.Count);
        }

        [Test]
        public async Task Should_Dispatch_according_to_max_retries_when_dispatch_fails()
        {
            var options = new UnsubscribeOptions();
            var state = options.GetExtensions().GetOrCreate<MessageDrivenUnsubscribeConnector.Settings>();
            state.MaxRetries = 10;
            state.RetryDelay = TimeSpan.Zero;
            dispatcher.FailDispatch(10);

            var context = new TestableUnsubscribeContext
            {
                Extensions = options.Context
            };

            await connector.Invoke(context, () => Task.FromResult(0), Dispatch);

            Assert.AreEqual(1, dispatcher.DispatchedTransportOperations.Count);
            Assert.AreEqual(10, dispatcher.FailedNumberOfTimes);
        }

        [Test]
        public void Should_Throw_when_max_retries_reached()
        {
            var options = new UnsubscribeOptions();
            var state = options.GetExtensions().GetOrCreate<MessageDrivenUnsubscribeConnector.Settings>();
            state.MaxRetries = 10;
            state.RetryDelay = TimeSpan.Zero;
            dispatcher.FailDispatch(11);

            var context = new TestableUnsubscribeContext
            {
                Extensions = options.Context
            };

            Assert.That(async () => await connector.Invoke(context, () => Task.FromResult(0), Dispatch), Throws.InstanceOf<QueueNotFoundException>());

            Assert.AreEqual(0, dispatcher.DispatchedTransportOperations.Count);
            Assert.AreEqual(11, dispatcher.FailedNumberOfTimes);
        }

        Task Dispatch(IUnicastRoutingContext c)
        {
            return dispatcher.Dispatch(new TransportOperations(new TransportOperation(c.Message, new UnicastAddressTag("destination"))), new TransportTransaction(), c.Extensions);
        }

        class FakeDispatcher : IDispatchMessages
        {
            int? numberOfTimes;

            public int FailedNumberOfTimes { get; private set; } = 0;

            public List<TransportOperations> DispatchedTransportOperations { get; } = new List<TransportOperations>();

            public void FailDispatch(int times)
            {
                numberOfTimes = times;
            }

            public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, ContextBag context)
            {
                if (numberOfTimes.HasValue && FailedNumberOfTimes < numberOfTimes.Value)
                {
                    FailedNumberOfTimes++;
                    throw new QueueNotFoundException();
                }

                DispatchedTransportOperations.Add(outgoingMessages);
                return TaskEx.CompletedTask;
            }
        }
    }
}