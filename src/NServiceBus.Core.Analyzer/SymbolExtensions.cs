namespace NServiceBus.Core.Analyzer;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

static class SymbolExtensions
{
    extension(ISymbol symbol)
    {
        public ITypeSymbol GetTypeSymbolOrDefault() => symbol switch
        {
            IDiscardSymbol symbolWithType => symbolWithType.Type,
            IEventSymbol symbolWithType => symbolWithType.Type,
            IFieldSymbol symbolWithType => symbolWithType.Type,
            ILocalSymbol symbolWithType => symbolWithType.Type,
            IMethodSymbol symbolWithType => symbolWithType.ReturnType,
            INamedTypeSymbol symbolWithType => symbolWithType,
            IParameterSymbol symbolWithType => symbolWithType.Type,
            IPointerTypeSymbol symbolWithType => symbolWithType.PointedAtType,
            IPropertySymbol symbolWithType => symbolWithType.Type,
            ITypeSymbol symbolWithType => symbolWithType,
            _ => null,
        };

        public IEnumerable<TNode> GetDescendantsAcrossDeclarations<TNode>(CancellationToken cancellationToken = default)
            where TNode : CSharpSyntaxNode
        {
            foreach (var syntaxRef in GetAllDeclaringSyntaxReferences(symbol))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var declarationNode = syntaxRef.GetSyntax(cancellationToken);
                foreach (var node in declarationNode.DescendantNodes().OfType<TNode>())
                {
                    yield return node;
                }
            }
        }
    }

    static ImmutableArray<SyntaxReference> GetAllDeclaringSyntaxReferences(ISymbol symbol)
    {
        return symbol switch
        {
            ITypeSymbol type => type.DeclaringSyntaxReferences,
            IMethodSymbol method => MergePartial(method),
            _ => symbol.DeclaringSyntaxReferences,
        };

        static ImmutableArray<SyntaxReference> MergePartial(IMethodSymbol method)
        {
            var def = method.PartialDefinitionPart?.DeclaringSyntaxReferences ?? [];
            var impl = method.PartialImplementationPart?.DeclaringSyntaxReferences ?? [];
            var self = method.DeclaringSyntaxReferences;
            return [.. ((SyntaxReference[])[.. def, .. impl, .. self]).Distinct()];
        }
    }
}