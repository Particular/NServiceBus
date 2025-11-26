#nullable enable
namespace NServiceBus.Core.Analyzer.Handlers;

readonly record struct RegistrationSpec(string AddType, string MessageType);