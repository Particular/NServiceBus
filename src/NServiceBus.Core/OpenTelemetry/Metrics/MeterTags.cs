namespace NServiceBus;

static class MeterTags
{
    public const string EndpointDiscriminator = "nservicebus.discriminator";
    public const string QueueName = "nservicebus.queue";
    public const string MessageType = "nservicebus.message_type";
    public const string FailureType = "nservicebus.failure_type";
    // used when publishing TotalProcessedSuccessfully and TotalFailures
    // to record all the message handlers invoked during the message processing
    public const string MessageHandlerTypes = "nservicebus.message_handler_types";
    // used when publishing MessageHandlerTime to record the invoked handler
    public const string MessageHandlerType = "nservicebus.message_handler_type";
    public const string ExecutionResult = "execution.result";
    public const string ErrorType = "error.type";
}