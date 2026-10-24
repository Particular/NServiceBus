#nullable enable

namespace NServiceBus.Core.Analyzer;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

static class HandlerRegistryCompositionOptions
{
    public const string PropertyName = "NServiceBusHandlerRegistryComposition";

    public static IncrementalValueProvider<bool> ParticipatesProvider(IncrementalGeneratorInitializationContext context) =>
        context.AnalyzerConfigOptionsProvider.Select(static (provider, _) => Participates(provider.GlobalOptions));

    public static bool Participates(AnalyzerConfigOptions globalOptions) =>
        !globalOptions.TryGetValue($"build_property.{PropertyName}", out var value) ||
        !bool.TryParse(value, out var enabled) ||
        enabled;
}
