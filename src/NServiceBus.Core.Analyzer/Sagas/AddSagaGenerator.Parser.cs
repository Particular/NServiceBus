#nullable enable

namespace NServiceBus.Core.Analyzer.Sagas;

using System.Threading;
using Handlers;
using Microsoft.CodeAnalysis;

public partial class AddSagaGenerator
{
    internal static class Parser
    {
        // Parsed in the transform so only the equatable spec flows through the pipeline, never the symbol or the semantic model.
        public static Sagas.SagaSpec? Parse(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken = default) =>
            HandlerKnownTypes.TryGet(context.SemanticModel.Compilation, out var knownTypes)
                ? Sagas.Parser.Parse(context.SemanticModel, (INamedTypeSymbol)context.TargetSymbol, knownTypes, cancellationToken)
                : null;
    }
}
