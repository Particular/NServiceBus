namespace NServiceBus.Core.Tests.Timeout.TimeoutManager
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;
    using NUnit.Framework;

    public class TimeoutRecoverabilityBehaviourTest
    {
        [Test]
        public async Task Invoke_when_timeout_fails_should_invoke_bus_notifications()
        {
            var messageDispatcher = new FakeMessageDispatcher();
            var busNotifications = new BusNotifications();
            var criticalError = new CriticalError(null);
            var behaviour = new TimeoutRecoverabilityBehavior("timeout_error_queue", "some_queue", messageDispatcher, busNotifications, criticalError);

            var flrAttempts = 0;
            busNotifications.Errors.MessageHasFailedAFirstLevelRetryAttempt += (sender, retry) => { flrAttempts = retry.RetryAttempt; };

            var messageMovedToErrorQueue = false;
            busNotifications.Errors.MessageSentToErrorQueue += (sender, message) => messageMovedToErrorQueue = true;

            var receiveContext = CreateContext();

            for (var i = 0; i < 6; ++i)
            {
                await behaviour.Invoke(receiveContext, SimulateTimoutFailure);
            }

            Assert.AreEqual(5, flrAttempts, "Expected a fixed number of FLR attempts");
            Assert.IsTrue(messageMovedToErrorQueue, "Expected message to be moved to the error queue");
        }

        static Task SimulateTimoutFailure()
        {
            throw new Exception("Simulated timeout failure");
        }

        ITransportReceiveContext CreateContext()
        {
            var messageId = Guid.NewGuid().ToString("D");
            var headers = new Dictionary<string, string>();

            return new TransportReceiveContext(
                messageId, headers, Stream.Null, 
                new TransportTransaction(), 
                new CancellationTokenSource(), 
                null);
        }
    }
}