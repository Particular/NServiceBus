namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Faults;

    public class MessagesFailedException : Exception
    {
        public MessagesFailedException(IList<FailedMessage> failedMessages, ScenarioContext scenarioContext) : base("One or more messages have been moved to the error queue.")
        {
            ScenarioContext = scenarioContext;
            FailedMessages = new ReadOnlyCollection<FailedMessage>(failedMessages);
        }

        public ScenarioContext ScenarioContext { get; }

        public IReadOnlyCollection<FailedMessage> FailedMessages { get; }
    }
}