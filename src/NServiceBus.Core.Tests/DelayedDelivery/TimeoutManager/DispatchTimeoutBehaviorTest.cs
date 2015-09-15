namespace NServiceBus.Core.Tests.Timeout.TimeoutManager
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using NServiceBus.InMemory.TimeoutPersister;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Timeout.Core;
    using NServiceBus.Transports;
    using NUnit.Framework;

    public class DispatchTimeoutBehaviorTest
    {
        [Test]
        public async Task Terminate_should_remove_timeout_from_timeout_storage()
        {
            var messageDispatcher = new FakeMessageDispatcher();
            var timeoutPersister = new InMemoryTimeoutPersister();
            var testee = new DispatchTimeoutBehavior(messageDispatcher, timeoutPersister);
            var timeoutId = await timeoutPersister.Add(CreateTimeout(), null);

            testee.Terminate(CreateContext(timeoutId));

            var result = await timeoutPersister.Peek(timeoutId, null);
            Assert.Null(result);
        }

        [Test]
        public async Task Terminate_when_dispatching_message_fails_should_not_remove_timeout()
        {
            var messageDispatcher = new FakeMessageDispatcher { DispatchFails = true };
            var timeoutPersister = new InMemoryTimeoutPersister();
            var testee = new DispatchTimeoutBehavior(messageDispatcher, timeoutPersister);
            var timeoutId = await timeoutPersister.Add(CreateTimeout(), null);

            Assert.Throws<Exception>(() => testee.Terminate(CreateContext(timeoutId)));

            var result = await timeoutPersister.Peek(timeoutId, null);
            Assert.NotNull(result);
        }

        static TimeoutData CreateTimeout()
        {
            return new TimeoutData
            {
                Destination = "endpointQueue",
                Headers = new Dictionary<string, string>()
            };
        }

        static PhysicalMessageProcessingStageBehavior.Context CreateContext(string timeoutId)
        {
            var messageId = Guid.NewGuid().ToString("D");
            var headers = new Dictionary<string, string>
            {
                {"Timeout.Id", timeoutId}
            };

            return new PhysicalMessageProcessingStageBehavior.Context(
                new TransportReceiveContext(
                    new IncomingMessage(messageId, headers, new MemoryStream()),
                    null));
        }

        class FakeMessageDispatcher : IDispatchMessages
        {
            public bool DispatchFails { get; set; }

            public void Dispatch(OutgoingMessage message, DispatchOptions dispatchOptions)
            {
                if (DispatchFails)
                {
                    throw new Exception("simulated exception");
                }
            }
        }
    }
}