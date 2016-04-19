namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Faults;

    public class MessagesFailedException : Exception
    {
        public MessagesFailedException(ScenarioContext scenarioContext) : base("One or more messages have been moved to the error queue.")
        {
            ScenarioContext = scenarioContext;
            FailedMessages = new ReadOnlyCollection<FailedMessage>(ScenarioContext.FailedMessages.Values.SelectMany(f => f).ToList());
        }

        public ScenarioContext ScenarioContext { get; }

        public IReadOnlyCollection<FailedMessage> FailedMessages { get; }
    }
}