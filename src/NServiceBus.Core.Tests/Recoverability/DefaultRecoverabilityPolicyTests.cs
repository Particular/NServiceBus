namespace NServiceBus.Core.Tests.Recoverability
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Extensibility;
    using NUnit.Framework;
    using Transport;

    [TestFixture]
    public class DefaultRecoverabilityPolicyTests
    {
        [Test]
        public void When_failure_is_assignable_to_custom_exception_should_move_to_error()
        {
            var policy = CreatePolicy(maxImmediateRetries: 3, maxDelayedRetries: 3, unrecoverableExceptions: new HashSet<Type> { typeof(MyBaseCustomException) });
            var errorContext = CreateErrorContext(numberOfDeliveryAttempts: 1, exception: new MyCustomException());

            var recoverabilityAction = policy(errorContext);

            Assert.IsInstanceOf<MoveToError>(recoverabilityAction, "Should move custom exception directly to error.");
        }

        class MyBaseCustomException : Exception
        {
        }

        class MyCustomException : MyBaseCustomException
        {
        }

        [Test]
        public void When_max_immediate_retries_have_not_been_reached_should_return_immediate_retry()
        {
            var policy = CreatePolicy(maxImmediateRetries: 3);
            var errorContext = CreateErrorContext(numberOfDeliveryAttempts: 2);

            var recoverabilityAction = policy(errorContext);

            Assert.IsInstanceOf<ImmediateRetry>(recoverabilityAction, "Should have one immediate retry left. It is second delivery attempt and we configured immediate reties to 2.");
        }

        [Test]
        public void When_max_immediate_retries_exceeded_should_return_delayed_retry()
        {
            var policy = CreatePolicy(2);
            var errorContext = CreateErrorContext(numberOfDeliveryAttempts: 3);

            var recoverabilityAction = policy(errorContext);

            Assert.IsInstanceOf<DelayedRetry>(recoverabilityAction, "When max number of immediate retries exceeded should return DelayedRetry.");
        }

        [Test]
        public void When_max_immediate_retries_exceeded_but_delayed_retry_disabled_return_move_to_error()
        {
            var policy = CreatePolicy(maxImmediateRetries: 1, maxDelayedRetries: 0);
            var errorContext = CreateErrorContext(numberOfDeliveryAttempts: 3);

            var recoverabilityAction = policy(errorContext);

            Assert.IsInstanceOf<MoveToError>(recoverabilityAction, "When max number of immediate retries exceeded and delayed retry disabled should return MoveToErrors.");
        }

        [Test]
        public void When_immediate_retries_turned_off_and_delayed_retry_policy_returns_delay_should_return_delayed_retry()
        {
            var deliveryDelay = TimeSpan.FromSeconds(10);
            var policy = CreatePolicy(maxImmediateRetries: 0, delayedRetryDelay: deliveryDelay);
            var errorContext = CreateErrorContext(numberOfDeliveryAttempts: 1);

            var recoverabilityAction = policy(errorContext);
            var delayedRetryAction = recoverabilityAction as DelayedRetry;

            Assert.IsInstanceOf<DelayedRetry>(recoverabilityAction, "When immediate retries turned off and delayed retries left, recoverability policy should return DelayedRetry");
            Assert.AreEqual(deliveryDelay, delayedRetryAction.Delay);
        }

        [Test]
        public void When_immediate_retries_turned_off_and_delayed_retries_turned_off_should_return_move_to_errors()
        {
            var policy = CreatePolicy(maxImmediateRetries: 0, maxDelayedRetries: 0);
            var errorContext = CreateErrorContext();

            var recoverabilityAction = policy(errorContext);

            Assert.IsInstanceOf<MoveToError>(recoverabilityAction, "When Immediate Retries turned off and Delayed Retry turned off should return MoveToErrors");
        }

        [Test]
        public void When_immediate_retries_turned_off_and_delayed_retry_policy_returns_no_delay_should_return_move_to_errors()
        {
            var policy = CreatePolicy(maxImmediateRetries: 0, maxDelayedRetries: 0, delayedRetryDelay: TimeSpan.Zero);
            var errorContext = CreateErrorContext();

            var recoverabilityAction = policy(errorContext);

            Assert.IsInstanceOf<MoveToError>(recoverabilityAction, "When Immediate Retries turned off and Delayed Retries policy returns no delay should return MoveToErrors");
        }

        [Test]
        public void When_immediate_retries_turned_off_and_delayed_retry_not_available_should_return_move_to_errors()
        {
            var policy = CreatePolicy(maxImmediateRetries: 0, maxDelayedRetries: 0);
            var errorContext = CreateErrorContext();

            var recoverabilityAction = policy(errorContext);

            Assert.IsInstanceOf<MoveToError>(recoverabilityAction, "When immediate retries turned off and delayed retries disabled should return MoveToErrors");
        }

        [Test]
        public void When_delayed_retry_counter_header_exists_recoverability_policy_should_use_it()
        {
            var policy = CreatePolicy(maxImmediateRetries: 0, maxDelayedRetries: 1, delayedRetryDelay: TimeSpan.Zero);
            var errorContext = CreateErrorContext(retryNumber: 1);

            var recoverabilityAction = policy(errorContext);

            Assert.IsInstanceOf<MoveToError>(recoverabilityAction, "When Delayed Retries cunter in headers reaches max delayed retries, policy should return MoveToErrors");
        }

        [Test]
        public void ShouldRetryTheSpecifiedTimesWithIncreasedDelay()
        {
            var baseDelay = TimeSpan.FromSeconds(10);
            var policy = CreatePolicy(maxImmediateRetries: 0, maxDelayedRetries: 2, delayedRetryDelay: baseDelay);

            var errorContext = CreateErrorContext(retryNumber: 0);
            var result1 = (DelayedRetry)policy(errorContext);

            errorContext = CreateErrorContext(retryNumber: 1);
            var result2 = (DelayedRetry)policy(errorContext);

            errorContext = CreateErrorContext(retryNumber: 2);
            var result3 = policy(errorContext);

            Assert.AreEqual(baseDelay, result1.Delay);
            Assert.AreEqual(TimeSpan.FromSeconds(20), result2.Delay);
            Assert.IsInstanceOf<MoveToError>(result3);
        }

        [Test]
        public void ShouldCapTheRetryMaxTimeTo24Hours()
        {
            var now = DateTimeOffset.UtcNow;
            var baseDelay = TimeSpan.FromSeconds(10);

            var policy = CreatePolicy(maxImmediateRetries: 0, maxDelayedRetries: 2, delayedRetryDelay: baseDelay);

            var moreThanADayAgo = now.AddHours(-24).AddTicks(-1);
            var headers = new Dictionary<string, string>
            {
                {Headers.DelayedRetriesTimestamp, DateTimeOffsetHelper.ToWireFormattedString(moreThanADayAgo)}
            };

            var errorContext = CreateErrorContext(headers: headers);

            var result = policy(errorContext);

            Assert.IsInstanceOf<MoveToError>(result);
        }

        ErrorContext CreateErrorContext(int numberOfDeliveryAttempts = 1, int? retryNumber = null, Dictionary<string, string> headers = null, Exception exception = null) =>
            new ErrorContext(
                exception ?? new Exception(),
                retryNumber.HasValue
                    ? new Dictionary<string, string> { { Headers.DelayedRetries, retryNumber.ToString() } }
                    : headers ?? new Dictionary<string, string>(),
                "message-id",
                new byte[0],
                new TransportTransaction(),
                numberOfDeliveryAttempts,
                "my-queue",
                new ContextBag());

        static Func<ErrorContext, RecoverabilityAction> CreatePolicy(int maxImmediateRetries = 2, int maxDelayedRetries = 2, TimeSpan? delayedRetryDelay = null, HashSet<Type> unrecoverableExceptions = null)
        {
            var failedConfig = new FailedConfig("errorQueue", unrecoverableExceptions ?? new HashSet<Type>());
            var config = new RecoverabilityConfig(new ImmediateConfig(maxImmediateRetries), new DelayedConfig(maxDelayedRetries, delayedRetryDelay.GetValueOrDefault(TimeSpan.FromSeconds(2))), failedConfig);
            return context => DefaultRecoverabilityPolicy.Invoke(config, context);
        }
    }
}
