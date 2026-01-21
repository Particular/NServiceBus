#nullable enable
namespace NServiceBus.Core.Analyzer.Handlers;

using System.Threading;
using Microsoft.CodeAnalysis;
using static Handlers;

public sealed partial class AddHandlerGenerator
{
    internal static class Parser
    {
        public static HandlerSpec? Parse(GeneratorAttributeSyntaxContext ctx, CancellationToken cancellationToken = default)
            => ctx.TargetSymbol is not INamedTypeSymbol namedTypeSymbol ? null : Handlers.Parser.Parse(ctx.SemanticModel, namedTypeSymbol, cancellationToken);
    }
}