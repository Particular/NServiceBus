#nullable enable

namespace NServiceBus;

// Metric tag names.
//
// All NServiceBus tags use the nservicebus.* namespace. Messaging semantic convention tags, such as
// messaging.destination.name, are left to the transport SDK instrumentation. An NServiceBus concept does
// not map one-to-one to the native one. The same tag name with two meanings makes a query match the
// wrong data.
//
// A general semantic convention tag is allowed where it has one meaning. Today that is only error.type.
// Its value is exception.GetType().FullName, as in ASP.NET Core. A base exception type is not a
// substitute for the type that occurred.
//
// See docs/decisions/2026-10-21-no-messaging-semantic-convention-attributes.md
static class MeterTags
{
    public const string EndpointDiscriminator = "nservicebus.discriminator";
    public const string QueueName = "nservicebus.queue";
    public const string MessageType = "nservicebus.message_type";
    public const string EnclosedMessageTypes = "nservicebus.enclosed_message_types";
    public const string MessageHandlerTypes = "nservicebus.message_handler_types";
    public const string MessageHandlerType = "nservicebus.message_handler_type";
    public const string ExecutionResult = "execution.result"; // pre-v11 tag, removed in v11, see obsoletes-v10.cs
    public const string ErrorType = "error.type"; // exception.GetType().FullName, see the note above
    public const string EnvelopeUnwrapperType = "nservicebus.envelope.unwrapper_type";
    public const string SagaType = "nservicebus.saga_type";
}