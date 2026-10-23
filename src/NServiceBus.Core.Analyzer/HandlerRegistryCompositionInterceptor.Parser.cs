#nullable enable

namespace NServiceBus.Core.Analyzer;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Utility;

public sealed partial class HandlerRegistryCompositionInterceptor
{
    internal enum CompositionKind
    {
        All,
        Handlers,
        Sagas
    }

    internal readonly record struct CallSpec(InterceptLocationSpec LocationSpec, CompositionKind Kind);

    internal readonly record struct EntrySpec(string AssemblyName, string EntryTypeName);

    internal readonly record struct CompositionSpec(ImmutableEquatableArray<CallSpec> Calls, ImmutableEquatableArray<EntrySpec> Entries);

    internal static class Parser
    {
        const string AddAllMethodName = "AddAll";
        const string AddAllHandlersMethodName = "AddAllHandlers";
        const string AddAllSagasMethodName = "AddAllSagas";
        const string GeneratedHandlerRegistryAttributeName = "NServiceBus.GeneratedHandlerRegistryAttribute";

        public static bool SyntaxLooksLikeCompositionCall(SyntaxNode node) => node is InvocationExpressionSyntax
        {
            Expression: MemberAccessExpressionSyntax
            {
                Name: IdentifierNameSyntax { Identifier.ValueText: AddAllMethodName or AddAllHandlersMethodName or AddAllSagasMethodName }
            },
            ArgumentList.Arguments.Count: 0
        };

        public static CallSpec? ParseCall(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken = default)
        {
            if (semanticModel.GetOperation(invocation, cancellationToken) is not IInvocationOperation { TargetMethod: { } method })
            {
                return null;
            }

            // Make sure the method is the one on the NServiceBus HandlerRegistry and not a copycat or one of the generated namespace registries
            if (!IsHandlerRegistryMethod(method) || GetKind(method.Name) is not { } kind)
            {
                return null;
            }

            if (semanticModel.GetInterceptableLocation(invocation, cancellationToken) is not { } location)
            {
                return null;
            }

            return new CallSpec(InterceptLocationSpec.From(location), kind);
        }

        static bool IsHandlerRegistryMethod(IMethodSymbol method) => method is
        {
            IsStatic: false,
            IsGenericMethod: false,
            Parameters.Length: 0,
            ContainingType:
            {
                Name: "HandlerRegistry",
                ContainingAssembly.Name: "NServiceBus.Core",
                ContainingNamespace:
                {
                    Name: "NServiceBus",
                    ContainingNamespace.IsGlobalNamespace: true
                }
            }
        };

        static CompositionKind? GetKind(string methodName) => methodName switch
        {
            AddAllMethodName => CompositionKind.All,
            AddAllHandlersMethodName => CompositionKind.Handlers,
            AddAllSagasMethodName => CompositionKind.Sagas,
            _ => null
        };

        public static ImmutableEquatableArray<EntrySpec> DiscoverReferencedEntries(Compilation compilation, CancellationToken cancellationToken = default)
        {
            var entries = new HashSet<EntrySpec>();

            foreach (var referencedAssembly in compilation.SourceModule.ReferencedAssemblySymbols)
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var attribute in referencedAssembly.GetAttributes())
                {
                    if (attribute.AttributeClass?.ToDisplayString() == GeneratedHandlerRegistryAttributeName &&
                        attribute.ConstructorArguments is [{ Value: INamedTypeSymbol { DeclaredAccessibility: Accessibility.Public, IsStatic: true } entryType }])
                    {
                        entries.Add(new EntrySpec(referencedAssembly.Name, entryType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                    }
                }
            }

            return Order(entries);
        }

        public static EntrySpec CreateLocalEntry(string assemblyName) =>
            new(assemblyName, $"global::{AddHandlerAndSagasRegistrationGenerator.Emitter.EntryNamespace}.{AddHandlerAndSagasRegistrationGenerator.Emitter.GetEntryTypeName(assemblyName)}");

        public static CompositionSpec CreateComposition(ImmutableEquatableArray<CallSpec> calls, ImmutableEquatableArray<EntrySpec> referencedEntries, EntrySpec? localEntry)
        {
            var entries = new HashSet<EntrySpec>(referencedEntries);
            if (localEntry is { } local)
            {
                entries.Add(local);
            }

            return new CompositionSpec(calls, Order(entries));
        }

        static ImmutableEquatableArray<EntrySpec> Order(IEnumerable<EntrySpec> entries) =>
            entries
                .OrderBy(static entry => entry.AssemblyName, StringComparer.Ordinal)
                .ThenBy(static entry => entry.EntryTypeName, StringComparer.Ordinal)
                .ToImmutableEquatableArray();
    }
}
