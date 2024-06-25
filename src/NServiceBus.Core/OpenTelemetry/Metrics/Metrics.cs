namespace NServiceBus;

static class Metrics
{
    public const string TotalProcessedSuccessfully = "nservicebus.messaging.successes";
    public const string TotalFetched = "nservicebus.messaging.fetches";
    public const string TotalFailures = "nservicebus.messaging.failures";
    public const string MessageHandlerTime = "nservicebus.messaging.handler_time";
}