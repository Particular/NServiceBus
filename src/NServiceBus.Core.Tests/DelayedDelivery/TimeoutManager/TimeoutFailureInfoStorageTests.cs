namespace NServiceBus.Core.Tests.Timeout.TimeoutManager
{
    using System;
    using NUnit.Framework;

    public class TimeoutFailureInfoStorageTests
    {
        [Test]
        public void When_recording_failure_initially_should_store_one_failed_attempt_and_exception()
        {
            var messageId = Guid.NewGuid().ToString("D");
            var exception = new Exception();

            var storage = new TimeoutFailureInfoStorage();

            storage.RecordFailureInfoForMessage(messageId, exception);
            
            var failureInfo = storage.GetFailureInfoForMessage(messageId);

            Assert.NotNull(failureInfo);
            Assert.AreEqual(1, failureInfo.NumberOfFailedAttempts);
            Assert.AreSame(exception, failureInfo.Exception);
        }

        [Test]
        public void When_recording_failure_many_times_should_store_number_of_attempts_and_last_exception()
        {
            var messageId = Guid.NewGuid().ToString("D");
            var secondException = new Exception();

            var storage = new TimeoutFailureInfoStorage();

            storage.RecordFailureInfoForMessage(messageId, new Exception());
            storage.RecordFailureInfoForMessage(messageId, secondException);

            var failureInfo = storage.GetFailureInfoForMessage(messageId);

            Assert.NotNull(failureInfo);
            Assert.AreEqual(2, failureInfo.NumberOfFailedAttempts);
            Assert.AreSame(secondException, failureInfo.Exception);
        }

        [Test]
        public void When_clearing_failure_should_return_null_on_subsequent_retrieval()
        {
            var messageId = Guid.NewGuid().ToString("D");

            var storage = new TimeoutFailureInfoStorage();

            storage.RecordFailureInfoForMessage(messageId, new Exception());
            
            var failureInfo = storage.GetFailureInfoForMessage(messageId);
            Assert.NotNull(failureInfo);
            
            storage.ClearFailureInfoForMessage(messageId);

            failureInfo = storage.GetFailureInfoForMessage(messageId);
            Assert.AreSame(TimeoutProcessingFailureInfo.NullFailureInfo, failureInfo);
        }

        [Test]
        public void When_recording_more_than_max_number_of_failures_should_remove_least_recently_used_entry()
        {
            const int MaxElements = 50;
            var storage = new TimeoutFailureInfoStorage(maxElements: MaxElements);

            var lruMessageId = Guid.NewGuid().ToString("D");

            storage.RecordFailureInfoForMessage(lruMessageId, new Exception());

            for (var i = 0; i < MaxElements; ++i)
            {
                var messageId = Guid.NewGuid().ToString("D");
                var exception = new Exception();

                storage.RecordFailureInfoForMessage(messageId, exception);
                storage.RecordFailureInfoForMessage(messageId, exception);
            }

            var lruFailureInfo = storage.GetFailureInfoForMessage(lruMessageId);
            Assert.AreSame(TimeoutProcessingFailureInfo.NullFailureInfo, lruFailureInfo);
        }
    }
}