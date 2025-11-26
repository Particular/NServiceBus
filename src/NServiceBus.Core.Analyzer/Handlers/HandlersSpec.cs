#nullable enable
namespace NServiceBus.Core.Analyzer.Handlers;

readonly record struct HandlersSpec(EquatableArray<HandlerSpec> Handlers);