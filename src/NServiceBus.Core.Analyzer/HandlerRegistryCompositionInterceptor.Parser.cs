#nullable enable

namespace NServiceBus.Core.Analyzer;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
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

    internal readonly record struct CallLocation(string FilePath, TextSpan TextSpan, LinePositionSpan LineSpan)
    {
        public Location ToLocation() => Location.Create(FilePath, TextSpan, LineSpan);
    }

    internal readonly record struct CallSpec(InterceptLocationSpec LocationSpec, CompositionKind Kind, CallLocation Location);

    internal readonly record struct EntrySpec(string AssemblyName, string EntryTypeName);

    internal readonly record struct CompositionSpec(ImmutableEquatableArray<CallSpec> Calls, ImmutableEquatableArray<EntrySpec> Entries);

    internal static class Parser
    {
        const string AddAllMethodName = "AddAll";
        const string AddAllHandlersMethodName = "AddAllHandlers";
        const string AddAllSagasMethodName = "AddAllSagas";
        const string NServiceBusCoreAssemblyName = "NServiceBus.Core";
        const string GeneratedHandlerRegistryAttributeName = "NServiceBus.GeneratedHandlerRegistryAttribute";

        public static bool SyntaxLooksLikeCompositionCall(SyntaxNode node) => node is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0 } invocation &&
            invocation.Expression switch
            {
                MemberAccessExpressionSyntax memberAccess => IsCompositionMethodName(memberAccess.Name),
                MemberBindingExpressionSyntax memberBinding => IsCompositionMethodName(memberBinding.Name),
                _ => false
            };

        static bool IsCompositionMethodName(SimpleNameSyntax name) => name is IdentifierNameSyntax { Identifier.ValueText: AddAllMethodName or AddAllHandlersMethodName or AddAllSagasMethodName };

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

            var invocationLocation = invocation.GetLocation();
            return new CallSpec(InterceptLocationSpec.From(location), kind, new CallLocation(invocationLocation.SourceTree?.FilePath ?? string.Empty, invocationLocation.SourceSpan, invocationLocation.GetLineSpan().Span));
        }

        public static bool IsCompositionMethod(IMethodSymbol method) => IsHandlerRegistryMethod(method) && GetKind(method.Name) is not null;

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

            if (compilation.GetTypeByMetadataName(GeneratedHandlerRegistryAttributeName) is not { } attributeType)
            {
                return ImmutableEquatableArray<EntrySpec>.Empty;
            }

            foreach (var referencedAssembly in compilation.SourceModule.ReferencedAssemblySymbols)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!ReferencesNServiceBusCore(referencedAssembly))
                {
                    continue;
                }

                foreach (var attribute in referencedAssembly.GetAttributes())
                {
                    if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType) &&
                        attribute.ConstructorArguments is [{ Value: INamedTypeSymbol { DeclaredAccessibility: Accessibility.Public, IsStatic: true } entryType }])
                    {
                        entries.Add(new EntrySpec(referencedAssembly.Name, entryType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                    }
                }
            }

            return Order(entries);
        }

        static bool ReferencesNServiceBusCore(IAssemblySymbol assembly)
        {
            foreach (var module in assembly.Modules)
            {
                foreach (var reference in module.ReferencedAssemblies)
                {
                    if (reference.Name == NServiceBusCoreAssemblyName)
                    {
                        return true;
                    }
                }
            }

            return false;
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
