namespace NServiceBus.Core.Tests.Timeout.TimeoutManager
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;
    using NUnit.Framework;

    public class TimeoutRecoverabilityBehaviorTest
    {
        [Test]
        public async Task Invoke_when_timeout_fails_should_invoke_bus_notifications()
        {
            var messageDispatcher = new FakeMessageDispatcher();
            var criticalError = new CriticalError(null);
            var behavior = new TimeoutRecoverabilityBehavior("timeout_error_queue", "some_queue", messageDispatcher, criticalError);

            var receiveContext = new FakeTransportReceiveContext();

            await behavior.Invoke(receiveContext, SimulateTimoutFailure);
            await behavior.Invoke(receiveContext, SimulateTimoutFailure);
            await behavior.Invoke(receiveContext, SimulateTimoutFailure);
            await behavior.Invoke(receiveContext, SimulateTimoutFailure);
            await behavior.Invoke(receiveContext, SimulateTimoutFailure);

            Assert.AreEqual(4, receiveContext.GetNotification<MessageToBeRetried>().Attempt, "Expected a fixed number of FLR attempts");

            await behavior.Invoke(receiveContext, SimulateTimoutFailure);

            Assert.NotNull(receiveContext.GetNotification<MessageFaulted>(), "Expected message to be moved to the error queue");
        }

        static Task SimulateTimoutFailure()
        {
            throw new Exception("Simulated timeout failure");
        }

        class FakeTransportReceiveContext : FakeBehaviorContext, ITransportReceiveContext
        {
            public FakeTransportReceiveContext()
            {
                Message = new IncomingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new MemoryStream());
            }

            public bool ReceiveOperationWasAborted { get; private set; }

            public IncomingMessage Message { get; }

            public void AbortReceiveOperation()
            {
                ReceiveOperationWasAborted = true;
            }
        }
    }
}