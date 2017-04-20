namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using Faults;

    public class MessageFailedException : Exception
    {
        public MessageFailedException(FailedMessage failedMessage, ScenarioContext scenarioContext)
            : base("A message has been moved to the error queue.", failedMessage.Exception)
        {
            ScenarioContext = scenarioContext;
            FailedMessage = failedMessage;
        }

        public ScenarioContext ScenarioContext { get; }

        public FailedMessage FailedMessage { get; }
    }
}