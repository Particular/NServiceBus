namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry;

using NUnit.Framework;

[NonParallelizable] // Ensure only activities for the current test are captured
public class OpenTelemetryAcceptanceTest : NServiceBusAcceptanceTest
{
}