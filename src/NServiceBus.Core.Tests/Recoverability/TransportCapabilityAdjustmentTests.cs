namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class TransportCapabilityAdjustmentTests
    {
        [Test]
        public void When_delayed_retries_not_supported_but_policy_demands_it_should_move_to_errors()
        {
            var recoverabilityAction = RecoverabilityComponent.AdjustForTransportCapabilities(ErrorQueueAddress, false, false, RecoverabilityAction.DelayedRetry(TimeSpan.FromSeconds(1)));

            var errorAction = recoverabilityAction as MoveToError;
            Assert.NotNull(errorAction);
            Assert.AreEqual(ErrorQueueAddress, errorAction.ErrorQueue);
        }

        [Test]
        public void When_immediate_retries_not_supported_but_policy_demands_it_should_move_to_errors()
        {
            var recoverabilityAction = RecoverabilityComponent.AdjustForTransportCapabilities(ErrorQueueAddress, false, false, RecoverabilityAction.ImmediateRetry());

            var errorAction = recoverabilityAction as MoveToError;
            Assert.NotNull(errorAction);
            Assert.AreEqual(ErrorQueueAddress, errorAction.ErrorQueue);
        }

        static string ErrorQueueAddress = "error-queue";
    }
}