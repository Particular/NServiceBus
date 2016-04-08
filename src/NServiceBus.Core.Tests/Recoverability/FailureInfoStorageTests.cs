namespace NServiceBus.Core.Tests.Timeout.TimeoutManager
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.ExceptionServices;
    using NUnit.Framework;

    public class FailureInfoStorageTests
    {
        [Test]
        public void When_recording_failure_initially_should_store_one_failed_attempt_and_exception()
        {
            var messageId = Guid.NewGuid().ToString("D");
            var exception = new Exception();
            var exceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception);

            var storage = GetFailureInfoStorage();

            storage.RecordFirstLevelRetryAttempt(messageId, exceptionDispatchInfo);
            
            var failureInfo = storage.GetFailureInfoForMessage(messageId);

            Assert.NotNull(failureInfo);
            Assert.AreEqual(1, failureInfo.FLRetries);
            Assert.AreSame(exception, failureInfo.Exception);
        }

        [Test]
        public void When_recording_failure_many_times_should_store_number_of_attempts_and_last_exception()
        {
            var messageId = Guid.NewGuid().ToString("D");
            var secondException = new Exception();

            var storage = GetFailureInfoStorage();

            storage.RecordFirstLevelRetryAttempt(messageId, ExceptionDispatchInfo.Capture(new Exception()));
            storage.RecordFirstLevelRetryAttempt(messageId, ExceptionDispatchInfo.Capture(secondException));

            var failureInfo = storage.GetFailureInfoForMessage(messageId);

            Assert.NotNull(failureInfo);
            Assert.AreEqual(2, failureInfo.FLRetries);
            Assert.AreSame(secondException, failureInfo.Exception);
        }

        [Test]
        public void When_clearing_failure_should_return_null_on_subsequent_retrieval()
        {
            var messageId = Guid.NewGuid().ToString("D");

            var storage = GetFailureInfoStorage();

            storage.RecordFirstLevelRetryAttempt(messageId, ExceptionDispatchInfo.Capture(new Exception()));
            
            var failureInfo = storage.GetFailureInfoForMessage(messageId);
            Assert.NotNull(failureInfo);
            
            storage.ClearFailureInfoForMessage(messageId);

            failureInfo = storage.GetFailureInfoForMessage(messageId);
            Assert.AreSame(ProcessingFailureInfo.NullFailureInfo, failureInfo);
        }

        [Test]
        public void When_recording_more_than_max_number_of_failures_should_remove_least_recently_used_entry()
        {
            const int MaxElements = 50;
            var storage = new FailureInfoStorage(maxElements: MaxElements);

            var lruMessageId = Guid.NewGuid().ToString("D");

            storage.RecordFirstLevelRetryAttempt(lruMessageId, ExceptionDispatchInfo.Capture(new Exception()));

            for (var i = 0; i < MaxElements; ++i)
            {
                var messageId = Guid.NewGuid().ToString("D");
                var exception = new Exception();

                storage.RecordFirstLevelRetryAttempt(messageId, ExceptionDispatchInfo.Capture(exception));
            }

            var lruFailureInfo = storage.GetFailureInfoForMessage(lruMessageId);
            Assert.AreSame(ProcessingFailureInfo.NullFailureInfo, lruFailureInfo);
        }

        [Test]
        public void When_recording_more_than_max_number_of_failures_many_times_should_remove_least_recently_used_entry()
        {
            const int MaxElements = 50;
            var storage = new FailureInfoStorage(MaxElements);

            var lruMessageId = Guid.NewGuid().ToString("D");

            storage.RecordFirstLevelRetryAttempt(lruMessageId, ExceptionDispatchInfo.Capture(new Exception()));

            var messageIds = new List<string>(MaxElements);
            for (var i = 0; i < MaxElements; ++i)
            {
                messageIds.Add(Guid.NewGuid().ToString("D"));
            }

            for (var i = 0; i < MaxElements - 1; ++i)
            {
                storage.RecordFirstLevelRetryAttempt(messageIds[i], ExceptionDispatchInfo.Capture(new Exception()));
            }

            storage.RecordFirstLevelRetryAttempt(lruMessageId, ExceptionDispatchInfo.Capture(new Exception()));

            storage.RecordFirstLevelRetryAttempt(messageIds[MaxElements - 1], ExceptionDispatchInfo.Capture(new Exception()));

            Assert.AreNotSame(ProcessingFailureInfo.NullFailureInfo, storage.GetFailureInfoForMessage(lruMessageId));
        }

        static FailureInfoStorage GetFailureInfoStorage()
        {
            return new FailureInfoStorage(10);
        }
    }
}