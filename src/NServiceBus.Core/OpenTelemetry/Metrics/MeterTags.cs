#nullable enable

namespace NServiceBus;

static class MeterTags
{
    public const string EndpointDiscriminator = "nservicebus.discriminator";
    public const string QueueName = "nservicebus.queue";
    public const string MessageType = "nservicebus.message_type";
    public const string MessageHandlerTypes = "nservicebus.message_handler_types";
    public const string MessageHandlerType = "nservicebus.message_handler_type";
    public const string ExecutionResult = "execution.result";
    public const string ErrorType = "error.type";
    public const string EnvelopeUnwrapperType = "nservicebus.envelope.unwrapper_type";
}