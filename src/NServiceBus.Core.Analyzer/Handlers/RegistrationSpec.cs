#nullable enable
namespace NServiceBus.Core.Analyzer.Handlers;

enum RegistrationType
{
    MessageHandler,
    StartMessageHandler,
    TimeoutHandler,
}

readonly record struct RegistrationSpec(RegistrationType RegistrationType, string MessageType);