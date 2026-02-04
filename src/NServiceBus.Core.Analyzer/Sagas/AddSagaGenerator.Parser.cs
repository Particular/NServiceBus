#nullable enable

namespace NServiceBus.Core.Analyzer.Sagas;

using System.Threading;
using Microsoft.CodeAnalysis;

public partial class AddSagaGenerator
{
    internal static class Parser
    {
        public static Sagas.SagaSpec? Parse(GeneratorAttributeSyntaxContext ctx, CancellationToken cancellationToken = default)
            => ctx.TargetSymbol is not INamedTypeSymbol namedTypeSymbol ? null : Sagas.Parser.Parse(ctx.SemanticModel, namedTypeSymbol, cancellationToken);
    }
}