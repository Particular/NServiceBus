namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Transport;
    using NUnit.Framework;

    [TestFixture]
    public class DefaultRecoverabilityPolicyTests
    {
        [Test]
        public void When_max_immediate_reties_have_not_been_reached_should_return_immediate_retry()
        {
            var policy = CreatePolicy(maxImmediateRetries: 3);
            var errorContext = CreateErrorContext(numberOfDeliveryAttempts: 2);

            var recoverabilityAction = policy.Invoke(errorContext);

            Assert.IsInstanceOf<ImmediateRetry>(recoverabilityAction, "We should have one immediate retry left. It is second delivery attempt and we configured immediate reties to 2.");
        }

        [Test]
        public void When_max_immediate_retries_exceeded_should_return_delayed_retry()
        {
            var policy = CreatePolicy(maxImmediateRetries: 1);
            var errorContext = CreateErrorContext(numberOfDeliveryAttempts: 3);

            var recoverabilityAction = policy.Invoke(errorContext);

            Assert.IsInstanceOf<DelayedRetry>(recoverabilityAction, "When max number of immediate retries exceeded should return DelayedRetry.");
        }

        [Test]
        public void When_max_immediate_retries_exceeded_but_delayed_retry_disabled_return_delayed_retry()
        {
            var policy = CreatePolicy(maxImmediateRetries: 1, delayedRetriesEnabled: false);
            var errorContext = CreateErrorContext(numberOfDeliveryAttempts: 3);

            var recoverabilityAction = policy.Invoke(errorContext);

            Assert.IsInstanceOf<MoveToError>(recoverabilityAction, "When max number of immediate retries exceeded and delayed retry disabled should return MoveToErrors.");
        }

        [Test]
        public void When_immediate_retries_turned_off_and_slr_policy_returns_delay_should_return_delayed_retry()
        {
            var deliveryDelay = TimeSpan.FromSeconds(10);
            var policy = CreatePolicy(maxImmediateRetries: 0, delayedRetryDelay: deliveryDelay);
            var errorContext = CreateErrorContext(numberOfDeliveryAttempts: 1);

            var recoverabilityAction = policy.Invoke(errorContext);
            var delayedRetryAction = recoverabilityAction as DelayedRetry;

            Assert.IsInstanceOf<DelayedRetry>(recoverabilityAction, "When immediate retries turned off and delayed retries left, recoverability policy should return DelayedRetry");
            Assert.AreEqual(deliveryDelay, delayedRetryAction.Delay);
        }

        [Test]
        public void When_immediate_retries_turned_off_and_slr_policy_returns_no_delay_should_return_move_to_errors()
        {
            var policy = CreatePolicy(maxImmediateRetries: 0, maxDelayedRetries: 0, delayedRetryDelay: TimeSpan.Zero);
            var errorContext = CreateErrorContext();

            var recoverabilityAction = policy.Invoke(errorContext);

            Assert.IsInstanceOf<MoveToError>(recoverabilityAction, "When immediate retries turned off and slr policy returns no delay should return MoveToErrors");
        }

        [Test]
        public void When_immediate_retries_turned_off_and_delayed_retry_not_available_should_return_move_to_errors()
        {
            var policy = CreatePolicy(maxImmediateRetries: 0, delayedRetriesEnabled: false);
            var errorContext = CreateErrorContext();

            var recoverabilityAction = policy.Invoke(errorContext);

            Assert.IsInstanceOf<MoveToError>(recoverabilityAction, "When immediate retries turned off and delayed retries disabled should return MoveToErrors");
        }

        [Test]
        public void When_slr_counter_header_exists_recoverability_policy_should_use_it()
        {
            var policy = CreatePolicy(maxImmediateRetries: 0, maxDelayedRetries: 1, delayedRetryDelay: TimeSpan.Zero);
            var errorContext = CreateErrorContext(headers: new Dictionary<string, string> { { Headers.Retries, "1" } });

            var recoverabilityAction = policy.Invoke(errorContext);

            Assert.IsInstanceOf<MoveToError>(recoverabilityAction, "When slr cunter in headers reaches max slr retries, policy should return MoveToErrors");
        }

        ErrorContext CreateErrorContext(int numberOfDeliveryAttempts = 0, Dictionary<string, string> headers = null)
        {
            return new ErrorContext(new Exception(), headers ?? new Dictionary<string, string>(), "message-id", new MemoryStream(), new TransportTransaction(), numberOfDeliveryAttempts);
        }

        DefaultRecoverabilityPolicy CreatePolicy(int maxImmediateRetries = 2, int maxDelayedRetries = 2, TimeSpan? delayedRetryDelay = null, bool delayedRetriesEnabled = true)
        {
            return new DefaultRecoverabilityPolicy(maxImmediateRetries > 0, delayedRetriesEnabled, maxImmediateRetries, new DefaultSecondLevelRetryPolicy(maxDelayedRetries, delayedRetryDelay ?? TimeSpan.FromSeconds(2)));
        }
    }
}