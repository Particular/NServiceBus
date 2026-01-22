#nullable enable

namespace NServiceBus.Core.Analyzer;

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

public partial class AddHandlerAndSagasRegistrationGenerator
{
    internal static class Parser
    {
        internal record BaseSpec(string Name, string Namespace, string AssemblyName, string FullyQualifiedName);

        public static BaseSpec? Parse(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken = default) => context.TargetSymbol is not INamedTypeSymbol namedTypeSymbol ? null : Parse(namedTypeSymbol);

        public static BaseSpec Parse(INamedTypeSymbol namedTypeSymbol)
        {
            var fullyQualifiedName = namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var displayParts = namedTypeSymbol.ToDisplayParts(SymbolDisplayFormat.FullyQualifiedFormat);
            var handlerOrSagaName = string.Join(string.Empty, displayParts.Where(x => x.Kind == SymbolDisplayPartKind.ClassName));
            var handlerOrSagaNamespace = GetNamespace(displayParts);
            var assemblyName = namedTypeSymbol.ContainingAssembly?.Name ?? "Assembly";
            return new BaseSpec(Name: handlerOrSagaName, Namespace: handlerOrSagaNamespace, AssemblyName: assemblyName, FullyQualifiedName: fullyQualifiedName);
        }

        static string GetNamespace(ImmutableArray<SymbolDisplayPart> handlerType) => handlerType.Length == 0 ? string.Empty : string.Join(".", handlerType.Where(x => x.Kind == SymbolDisplayPartKind.NamespaceName));
    }
}