#nullable enable

namespace NServiceBus.AcceptanceTesting.Support;

using System;
using Faults;

public class MessageFailedException(FailedMessage failedMessage, ScenarioContext scenarioContext)
    : Exception("A message has been moved to the error queue.", failedMessage.Exception)
{
    public ScenarioContext ScenarioContext { get; } = scenarioContext;

    public FailedMessage FailedMessage { get; } = failedMessage;

    // Show the stack trace of the exception which caused the message to fail
    public override string? StackTrace => FailedMessage.Exception.StackTrace;
}