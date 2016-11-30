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
    using NUnit.Framework;
    using Testing;
    using Unicast.Queuing;

    [TestFixture]
    public class MessageDrivenSubscribeConnectorTests
    {
        [SetUp]
        public void SetUp()
        {
            publishers = new Publishers();
            publishers.AddOrReplacePublishers("A", new List<PublisherTableEntry> {new PublisherTableEntry(typeof(object), PublisherAddress.CreateFromPhysicalAddresses("publisher1"))});
            dispatcher = new FakeDispatcher();
            subscribeConnector = new MessageDrivenSubscribeConnector(publishers, "replyToAddress", "Endpoint");
        }

        [Test]
        public async Task Should_include_TimeSent_and_Version_headers()
        {
            var unsubscribeTerminator = new MessageDrivenUnsubscribeConnector(publishers, "replyToAddress", "Endpoint");

            await subscribeConnector.Invoke(new TestableSubscribeContext(), () => Task.FromResult(0), Dispatch);
            await unsubscribeTerminator.Invoke(new TestableUnsubscribeContext(), () => Task.FromResult(0), Dispatch);

            foreach (var dispatchedTransportOperation in dispatcher.DispatchedTransportOperations)
            {
                var unicastTransportOperations = dispatchedTransportOperation.UnicastTransportOperations;
                var operations = new List<UnicastTransportOperation>(unicastTransportOperations);

                Assert.IsTrue(operations[0].Message.Headers.ContainsKey(Headers.TimeSent));
                Assert.IsTrue(operations[0].Message.Headers.ContainsKey(Headers.NServiceBusVersion));
            }
        }

        [Test]
        public async Task Should_Dispatch_for_all_publishers()
        {
            await subscribeConnector.Invoke(new TestableSubscribeContext(), () => Task.FromResult(0), Dispatch);

            Assert.AreEqual(1, dispatcher.DispatchedTransportOperations.Count);
        }

        [Test]
        public async Task Should_Dispatch_according_to_max_retries_when_dispatch_fails()
        {
            var context = new TestableSubscribeContext();
            var state = context.Extensions.GetOrCreate<MessageDrivenSubscribeConnector.Settings>();
            state.MaxRetries = 10;
            state.RetryDelay = TimeSpan.Zero;
            dispatcher.FailDispatch(10);

            await subscribeConnector.Invoke(context, () => Task.FromResult(0), Dispatch);

            Assert.AreEqual(1, dispatcher.DispatchedTransportOperations.Count);
            Assert.AreEqual(10, dispatcher.FailedNumberOfTimes);
        }

        [Test]
        public void Should_Throw_when_max_retries_reached()
        {
            var context = new TestableSubscribeContext();
            var state = context.Extensions.GetOrCreate<MessageDrivenSubscribeConnector.Settings>();
            state.MaxRetries = 10;
            state.RetryDelay = TimeSpan.Zero;
            dispatcher.FailDispatch(11);

            Assert.That(async () =>
            {
                await subscribeConnector.Invoke(context, () => Task.FromResult(0), Dispatch);
            }, Throws.InstanceOf<QueueNotFoundException>());

            Assert.AreEqual(0, dispatcher.DispatchedTransportOperations.Count);
            Assert.AreEqual(11, dispatcher.FailedNumberOfTimes);
        }

        Task Dispatch(IUnicastRoutingContext c)
        {
            return dispatcher.Dispatch(new TransportOperations(new TransportOperation(c.Message, new UnicastAddressTag("destination"))), new TransportTransaction(), c.Extensions);
        }

        FakeDispatcher dispatcher;
        MessageDrivenSubscribeConnector subscribeConnector;
        Publishers publishers;

        class FakeDispatcher : IDispatchMessages
        {
            public int FailedNumberOfTimes { get; private set; }

            public List<TransportOperations> DispatchedTransportOperations { get; } = new List<TransportOperations>();

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

            public void FailDispatch(int times)
            {
                numberOfTimes = times;
            }

            int? numberOfTimes;
        }
    }
}