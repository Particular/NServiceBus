namespace NServiceBus.Core.Analyzer;

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

public partial class AddHandlerAndSagasRegistrationGenerator
{
    internal static class Parser
    {
        internal readonly record struct HandlerOrSagaBaseSpec(string Namespace, string AssemblyName, string Type);

        public static HandlerOrSagaBaseSpec? Parse(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken = default) => context.TargetSymbol is not INamedTypeSymbol namedTypeSymbol ? null : Parse(namedTypeSymbol);

        static HandlerOrSagaBaseSpec Parse(INamedTypeSymbol namedTypeSymbol)
        {
            var fullyQualifiedName = namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var displayParts = namedTypeSymbol.ToDisplayParts(SymbolDisplayFormat.FullyQualifiedFormat);
            var handlerOrSagaNamespace = GetNamespace(displayParts);
            var assemblyName = namedTypeSymbol.ContainingAssembly?.Name ?? "Assembly";
            return new HandlerOrSagaBaseSpec(Namespace: handlerOrSagaNamespace, AssemblyName: assemblyName, Type: fullyQualifiedName);
        }

        static string GetNamespace(ImmutableArray<SymbolDisplayPart> handlerType) => handlerType.Length == 0 ? string.Empty : string.Join(".", handlerType.Where(x => x.Kind == SymbolDisplayPartKind.NamespaceName));
    }
}