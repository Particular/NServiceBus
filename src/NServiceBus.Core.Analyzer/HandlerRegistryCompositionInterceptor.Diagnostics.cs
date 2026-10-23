#nullable enable

namespace NServiceBus.Core.Analyzer;

using Microsoft.CodeAnalysis;

public sealed partial class HandlerRegistryCompositionInterceptor
{
    static readonly DiagnosticDescriptor NoHandlerRegistryFound = new(
        id: DiagnosticIds.NoHandlerRegistryFound,
        title: "No handler registry found",
        messageFormat: "HandlerRegistry.{0} registers nothing because neither this assembly nor a referenced assembly has a generated handler registry.",
        category: "Code",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
