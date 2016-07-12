namespace NServiceBus.Core.Tests.Msmq
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;

    public class MsmqFailureInfoStorageTests
    {
        [Test]
        public void When_recording_failure_initially_should_store_one_failed_attempt_and_exception()
        {
            var messageId = Guid.NewGuid().ToString("D");
            var exception = new Exception();

            var storage = GetFailureInfoStorage();

            storage.RecordFailureInfoForMessage(messageId, exception);

            MsmqFailureInfoStorage.ProcessingFailureInfo failureInfo;
            storage.TryGetFailureInfoForMessage(messageId, out failureInfo);

            Assert.NotNull(failureInfo);
            Assert.AreEqual(1, failureInfo.NumberOfProcessingAttempts);
            Assert.AreSame(exception, failureInfo.Exception);
        }

        [Test]
        public void When_recording_failure_many_times_should_store_number_of_attempts_and_last_exception()
        {
            var messageId = Guid.NewGuid().ToString("D");
            var secondException = new Exception();

            var storage = GetFailureInfoStorage();

            storage.RecordFailureInfoForMessage(messageId, new Exception());
            storage.RecordFailureInfoForMessage(messageId, secondException);

            MsmqFailureInfoStorage.ProcessingFailureInfo failureInfo;
            storage.TryGetFailureInfoForMessage(messageId, out failureInfo);

            Assert.NotNull(failureInfo);
            Assert.AreEqual(2, failureInfo.NumberOfProcessingAttempts);
            Assert.AreSame(secondException, failureInfo.Exception);
        }

        [Test]
        public void When_clearing_failure_should_return_null_on_subsequent_retrieval()
        {
            var messageId = Guid.NewGuid().ToString("D");

            var storage = GetFailureInfoStorage();

            storage.RecordFailureInfoForMessage(messageId, new Exception());

            MsmqFailureInfoStorage.ProcessingFailureInfo failureInfo;

            storage.TryGetFailureInfoForMessage(messageId, out failureInfo);
            Assert.NotNull(failureInfo);

            storage.ClearFailureInfoForMessage(messageId);

            storage.TryGetFailureInfoForMessage(messageId, out failureInfo);
            Assert.IsNull(failureInfo);
        }

        [Test]
        public void When_recording_more_than_max_number_of_failures_should_remove_least_recently_used_entry()
        {
            const int MaxElements = 50;
            var storage = new MsmqFailureInfoStorage(maxElements: MaxElements);

            var lruMessageId = Guid.NewGuid().ToString("D");

            storage.RecordFailureInfoForMessage(lruMessageId, new Exception());

            for (var i = 0; i < MaxElements; ++i)
            {
                var messageId = Guid.NewGuid().ToString("D");
                var exception = new Exception();

                storage.RecordFailureInfoForMessage(messageId, exception);
            }

            MsmqFailureInfoStorage.ProcessingFailureInfo failureInfo;
            storage.TryGetFailureInfoForMessage(lruMessageId, out failureInfo);

            Assert.IsNull(failureInfo);
        }

        [Test]
        public void When_recording_a_failure_for_a_message_it_should_not_be_treated_as_least_recently_used()
        {
            const int MaxElements = 50;
            var storage = new MsmqFailureInfoStorage(MaxElements);

            var lruMessageId = Guid.NewGuid().ToString("D");

            storage.RecordFailureInfoForMessage(lruMessageId, new Exception());

            var messageIds = new List<string>(MaxElements);
            for (var i = 0; i < MaxElements; ++i)
            {
                messageIds.Add(Guid.NewGuid().ToString("D"));
            }

            for (var i = 0; i < MaxElements - 1; ++i)
            {
                storage.RecordFailureInfoForMessage(messageIds[i], new Exception());
            }

            storage.RecordFailureInfoForMessage(lruMessageId, new Exception());

            storage.RecordFailureInfoForMessage(messageIds[MaxElements - 1], new Exception());

            MsmqFailureInfoStorage.ProcessingFailureInfo failureInfo;
            storage.TryGetFailureInfoForMessage(lruMessageId, out failureInfo);

            Assert.IsNotNull(failureInfo);
        }

        static MsmqFailureInfoStorage GetFailureInfoStorage()
        {
            return new MsmqFailureInfoStorage(10);
        }
    }
}