#nullable enable

namespace NServiceBus.Core.Analyzer;

using Handlers;
using Microsoft.CodeAnalysis;

static class KnownTypePipelines
{
    internal static IncrementalValueProvider<HandlerKnownTypes?> BuildHandlerKnownTypesPipeline(IncrementalGeneratorInitializationContext context) =>
        context.CompilationProvider
            .Select(static (compilation, _) =>
            {
                HandlerKnownTypes.TryGet(compilation, out var knownTypes);
                return knownTypes;
            });
}