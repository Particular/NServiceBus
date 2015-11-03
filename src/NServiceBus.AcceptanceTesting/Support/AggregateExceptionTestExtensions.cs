namespace NServiceBus.AcceptanceTesting.Support
{
    using System;

    public static class AggregateExceptionTestExtensions
    {
        public static MessagesFailedException ExpectFailedMessages(this AggregateException aggregateException)
        {
            var messagesFailedException = aggregateException.InnerException as MessagesFailedException;
            if (messagesFailedException != null)
            {
                return messagesFailedException;
            }

            throw new ArgumentException(
                "Expected AggregateException to contain a MessagesFailedException, but it did not.",
                nameof(aggregateException),
                aggregateException);
        }
    }
}