#nullable enable
namespace NServiceBus.Core.Analyzer.Handlers;

using Microsoft.CodeAnalysis;
using System.Threading;
using static Handlers;
using BaseParser = AddHandlerAndSagasRegistrationGenerator.Parser;

public sealed partial class AddHandlerGenerator
{
    internal static class Parser
    {
        public static HandlerSpec? Parse(GeneratorAttributeSyntaxContext ctx, CancellationToken cancellationToken = default)
            => ctx.TargetSymbol is not INamedTypeSymbol namedTypeSymbol
                ? null
                : Handlers.Parser.Parse(ctx.SemanticModel, namedTypeSymbol, BaseParser.SpecKind.Handler, cancellationToken: cancellationToken);
    }
}