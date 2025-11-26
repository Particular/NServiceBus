#nullable enable
namespace NServiceBus.Core.Analyzer.Handlers;

using Utility;

record HandlerSpec(
    InterceptLocationSpec LocationSpec,
    string Name,
    string HandlerType,
    EquatableArray<RegistrationSpec> Registrations);