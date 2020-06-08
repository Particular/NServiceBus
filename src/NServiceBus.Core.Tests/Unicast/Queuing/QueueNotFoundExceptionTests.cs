namespace NServiceBus.Unicast.Queuing
{
    using NUnit.Framework;

    public class QueueNotFoundExceptionTests
    {
        [Test]
        public void When_has_queue_should_mention_queue_in_error_message()
        {
            var queueName = "missing-queue";

            var exception = new QueueNotFoundException {Queue = queueName};

            Assert.AreEqual(queueName, exception.Queue);
            var expectedMessage = $"Queue '{queueName}' not found. This queue might need to be created manually.";
            Assert.AreEqual(expectedMessage, exception.Message);
            StringAssert.Contains(expectedMessage, exception.ToString());
        }

        [Test]
        public void When_has_no_queue_should_keep_error_message_generic()
        {
            var exception = new QueueNotFoundException();

            Assert.Null(exception.Queue);
            // This is the standard Exception message which is typically localized.
            var expectedMessage = "Exception of type 'NServiceBus.Unicast.Queuing.QueueNotFoundException' was thrown.";
            Assert.AreEqual(expectedMessage, exception.Message);
            StringAssert.Contains(expectedMessage, exception.ToString());
        }
    }
}