namespace NServiceBus.Core.Tests.Msmq
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Routing;
    using NServiceBus.Transports;
    using NServiceBus.Transports.Msmq;
    using NUnit.Framework;

    [TestFixture]
    public class MsmqMessageDispatcherTests
    {
        [Test]
        public async Task Should_set_label_when_convention_configured()
        {
            var queueName = "labelTest";
            var path = $@"{Environment.MachineName}\private$\{queueName}";
            try
            {
                MsmqHelpers.DeleteQueue(path);
                MsmqHelpers.CreateQueue(path);
                var messageSender = new MsmqMessageDispatcher(new MsmqSettings(), _ => "mylabel", new NullSubscriptionStore(), new Type[0]);

                var bytes = new byte[]
                {
                    1
                };
                var headers = new Dictionary<string, string>();
                var outgoingMessage = new OutgoingMessage("1", headers, bytes);
                var transportOperation = new TransportOperation(outgoingMessage, new UnicastAddressTag(queueName), DispatchConsistency.Default);
                await messageSender.Dispatch(new TransportOperations(transportOperation), new ContextBag()).ConfigureAwait(false);
                var messageLabel = ReadMessageLabel(path);
                Assert.AreEqual("mylabel", messageLabel);

            }
            finally
            {
                MsmqHelpers.DeleteQueue(path);
            }
        }
        [Test]
        public async Task Should_use_string_empty_label_when_no_convention_configured()
        {
            var queueName = "emptyLabelTest";
            var path = $@".\private$\{queueName}";
            try
            {
                MsmqHelpers.DeleteQueue(path);
                MsmqHelpers.CreateQueue(path);
                var messageSender = new MsmqMessageDispatcher(new MsmqSettings(), pairs => string.Empty, new NullSubscriptionStore(), new Type[0]);

                var bytes = new byte[]
                {
                    1
                };
                var headers = new Dictionary<string, string>();
                var outgoingMessage = new OutgoingMessage("1", headers, bytes);
                var transportOperation = new TransportOperation(outgoingMessage, new UnicastAddressTag(queueName), DispatchConsistency.Default);
                await messageSender.Dispatch(new TransportOperations(transportOperation), new ContextBag()).ConfigureAwait(false);
                var messageLabel = ReadMessageLabel(path);
                Assert.IsEmpty(messageLabel);

            }
            finally
            {
                MsmqHelpers.DeleteQueue(path);
            }
        }

        class NullSubscriptionStore : IQuerySubscriptions
        {
            public Task<IEnumerable<Subscriber>> GetSubscribersFor(IEnumerable<Type> eventTypes)
            {
                throw new NotImplementedException();
            }
        }

        static string ReadMessageLabel(string path)
        {
            using (var queue = new MessageQueue(path))
            using (var message = queue.Receive(TimeSpan.FromSeconds(5)))
            {
                return message?.Label;
            }
        }
    }
}