#nullable enable
namespace NServiceBus.Core.Analyzer;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[Generator]
public sealed class KnownTypesGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // STRATEGY: Keep sourceTypes as Plural (stream), collect markers into Singular (arrays)
        // WHY: Users frequently add/modify types (handlers, sagas) but markers rarely change.
        // When sourceTypes is Plural and a new type is added, only that ONE type flows through
        // the pipeline and gets checked against cached marker arrays. This is optimal for
        // the common case of iterative development.

        // Get all source types - keep as PLURAL stream for optimal incremental performance
        var sourceTypes = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is TypeDeclarationSyntax,
                transform: GetNamedTypeFromGeneratorSyntaxContext)
            .Where(type => type is not null);

        // --- THREE INDEPENDENT MARKER SOURCES ---
        // Each source is kept as a Plural stream until we collect it at the cache boundary.
        // This ensures changes to one marker source don't invalidate caches from other sources.

        // Marker source 1: [NServiceBusExtensionPoint] attributes in user's source code
        var markersFromSourceAttributes = context.SyntaxProvider
            .ForAttributeWithMetadataName(ExtensionAttributeMetadataName,
                predicate: (node, token) => true,
                transform: (syntaxContext, token) =>
                {
                    var attributeInfo = syntaxContext.Attributes.First();
                    if (syntaxContext.TargetSymbol is INamedTypeSymbol decoratedType &&
                        attributeInfo.ConstructorArguments[0].Value is string registrationMethodName)
                    {
                        return new MarkerTypeInfo(decoratedType, registrationMethodName);
                    }
                    return default;
                })
            .Where(x => x.Symbol is not null);

        // Marker source 2: [NServiceBusExtensionPoint] attributes in referenced assemblies
        var markersFromCompilationAttributes = context.CompilationProvider
            .SelectMany(GetTypesWithExtensionPointAttribute);

        // Marker source 3: Built-in marker types (IHandleMessages<T>, Saga<T>, IEvent, ICommand, etc.)
        var builtInMarkers = context.CompilationProvider
            .SelectMany((compilation, cancellationToken) =>
            {
                return MarkerTypeInfos
                    .Select(info => new MarkerTypeInfo(compilation.GetTypeByMetadataName(info.TypeName), info.RegisterMethod))
                    .Where(m => m.Symbol is not null);
            });

        // --- CACHE BOUNDARY: Collect each marker source independently ---
        // Collecting here creates separate cache entries for each marker source.
        // Changes to source attributes don't invalidate compilation or built-in marker caches.
        var collectedSourceMarkers = markersFromSourceAttributes.Collect();
        var collectedCompilationMarkers = markersFromCompilationAttributes.Collect();
        var collectedBuiltInMarkers = builtInMarkers.Collect();

        // --- MATCH TYPES AGAINST MARKERS ---
        // For each source type (Plural), pair it with each marker array (Singular).
        // The Combine API requires: Combine<Plural, Singular>, so we collected markers above.
        // Result: When a new type is added, it flows through as ONE item and gets checked
        // against the cached marker arrays - very efficient!
        var matchedTypesFromSourceAttributes = sourceTypes
            .Combine(collectedSourceMarkers)
            .SelectMany((pair, cancellationToken) => FindMatchingMarkers(pair.Left, pair.Right, cancellationToken));

        var matchedTypesFromCompilationAttributes = sourceTypes
            .Combine(collectedCompilationMarkers)
            .SelectMany((pair, cancellationToken) => FindMatchingMarkers(pair.Left, pair.Right, cancellationToken));

        var matchedTypesFromBuiltInMarkers = sourceTypes
            .Combine(collectedBuiltInMarkers)
            .SelectMany((pair, cancellationToken) => FindMatchingMarkers(pair.Left, pair.Right, cancellationToken));

        // --- CACHE BOUNDARY: Collect matched types from each source independently ---
        // Final collection before combining results - maintains separation between sources
        var collectedFromSource = matchedTypesFromSourceAttributes.Collect();
        var collectedFromCompilation = matchedTypesFromCompilationAttributes.Collect();
        var collectedFromBuiltIn = matchedTypesFromBuiltInMarkers.Collect();

        // --- FINAL COMBINATION ---
        // Only now do we merge all three result sets into one array for source generation
        var allScannedTypes = collectedFromSource
            .Combine(collectedFromCompilation)
            .Combine(collectedFromBuiltIn)
            .Select((tuple, _) =>
            {
                var ((source, compilation), builtIn) = tuple;
                return source.Concat(compilation).Concat(builtIn)
                    // CompilationProvider and SyntaxProvider can both surface an extension point type in user code
                    .Distinct()
                    .ToImmutableArray();
            });

        // --- GENERATE OUTPUT ---
        context.RegisterImplementationSourceOutput(allScannedTypes, GenerateRegistrationCode);
    }

    static IEnumerable<ScannedTypeInfo> FindMatchingMarkers(INamedTypeSymbol? type, ImmutableArray<MarkerTypeInfo> markers, CancellationToken cancellationToken)
    {
        if (type is null)
        {
            yield break;
        }

        // TODO: Is it really desired to have all returned? This currently results in events registered as events AND messages.
        // It would be more efficient to only return the first, but this would create an ordering problem in the baked-in list,
        // where you'd want to have IEvent and ICommand precede IMessage, or else sort the hard-coded list by inheritance
        // relationships?
        foreach (var marker in markers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (marker.Symbol is not null && IsFromExtensionPoint(type, marker.Symbol))
            {
                yield return new ScannedTypeInfo(type.ToDisplayString(), marker.RegisterMethod);
            }
        }
    }

    /// <auto-generated />
    static void GenerateRegistrationCode(SourceProductionContext sourceProductionContext, ImmutableArray<ScannedTypeInfo> matches)
    {
        if (matches.Length == 0)
        {
            return; // No types to register, don't generate anything
        }

        var sb = new StringBuilder();
        sb.AppendLine("""
                      // <auto-generated/>
                      #nullable enable
                      namespace NServiceBus.Generated
                      {
                          public static class TypeRegistration
                          {
                              public static void RegisterTypes(NServiceBus.EndpointConfiguration config)
                              {
                      """);

        foreach (var type in matches)
        {
            sb.AppendLine($"            config.{type.RegisterMethod}<{type.DisplayName}>();");
        }

        sb.AppendLine();
        sb.AppendLine("            // OR");
        foreach (var type in matches)
        {
            sb.AppendLine($"            config.RegisterExtensionType<{type.DisplayName}>();");
        }

        sb.AppendLine("""
                              }
                          }
                          
                          public static class TemporaryRegistrationExtensions
                          {
                              public static void RegisterExtensionType<T>(this NServiceBus.EndpointConfiguration config) { }
                      """);
        foreach (var methodName in matches.Select(m => m.RegisterMethod).Distinct())
        {
            sb.AppendLine($"        public static void {methodName}<TType>(this NServiceBus.EndpointConfiguration config) {{ }}");
        }
        sb.AppendLine("""
                          }
                      }
                      """);

        sourceProductionContext.AddSource("TypeRegistration.g.cs", sb.ToString());
    }

    static IEnumerable<MarkerTypeInfo> GetTypesWithExtensionPointAttribute(Compilation compilation, CancellationToken cancellationToken)
    {
        var attributeType = compilation.GetTypeByMetadataName(ExtensionAttributeMetadataName);
        if (attributeType is null)
        {
            return [];
        }

        return GetTypesWithExtensionPointAttribute(compilation.GlobalNamespace, attributeType, cancellationToken);
    }

    static IEnumerable<MarkerTypeInfo> GetTypesWithExtensionPointAttribute(INamespaceSymbol ns, INamedTypeSymbol attributeType, CancellationToken cancellationToken)
    {
        // Walk all types in this namespace and its children looking for [NServiceBusExtensionPoint]
        foreach (var type in ns.GetTypeMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var extensionPointAttr = type.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeType));

            if (extensionPointAttr is not null && extensionPointAttr.ConstructorArguments[0].Value is string registerMethod)
            {
                yield return new MarkerTypeInfo(type, registerMethod);
            }

            // Recursively search nested types
            foreach (var nested in GetNestedTypesWithAttribute(type, attributeType, cancellationToken))
            {
                yield return nested;
            }
        }

        // Recursively search child namespaces
        foreach (var childNamespace in ns.GetNamespaceMembers())
        {
            foreach (var markerType in GetTypesWithExtensionPointAttribute(childNamespace, attributeType, cancellationToken))
            {
                yield return markerType;
            }
        }
    }

    static IEnumerable<MarkerTypeInfo> GetNestedTypesWithAttribute(INamedTypeSymbol type, INamedTypeSymbol attributeType, CancellationToken cancellationToken)
    {
        foreach (var nested in type.GetTypeMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var extensionPointAttr = nested.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeType));

            if (extensionPointAttr is not null && extensionPointAttr.ConstructorArguments[0].Value is string registerMethod)
            {
                yield return new MarkerTypeInfo(nested, registerMethod);
            }

            // Recursively search deeper nested types
            foreach (var deeperNested in GetNestedTypesWithAttribute(nested, attributeType, cancellationToken))
            {
                yield return deeperNested;
            }
        }
    }

    static bool IsFromExtensionPoint(INamedTypeSymbol type, INamedTypeSymbol extensionMarkerType)
    {
        if (SymbolEqualityComparer.Default.Equals(type, extensionMarkerType))
        {
            // We don't want to get IEvent itself
            return false;
        }

        if (extensionMarkerType.TypeKind == TypeKind.Interface)
        {
            foreach (var iface in type.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, extensionMarkerType))
                {
                    return true;
                }
            }

            return false;
        }

        // Walk base types
        var baseType = type.BaseType;
        while (baseType != null)
        {
            if (SymbolEqualityComparer.Default.Equals(baseType.OriginalDefinition, extensionMarkerType))
            {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }

    // Built-in marker types that we always check for
    // These are converted to MarkerTypeInfo with actual symbols during generation
    static readonly ImmutableArray<BuiltInMarkerInfo> MarkerTypeInfos =
    [
        new ("NServiceBus.IEvent", "RegisterEvent"),
        new ("NServiceBus.ICommand", "RegisterCommand"),
        new ("NServiceBus.IMessage", "RegisterMessage"),
        new ("NServiceBus.IHandleMessages`1", "RegisterHandler"),
        new ("NServiceBus.Saga`1", "RegisterSaga"),
        new ("NServiceBus.IContainSagaData", "RegisterSagaData"),
        new ("NServiceBus.Installation.INeedToInstallSomething", "RegisterInstaller"),
        new ("NServiceBus.Features.Feature", "RegisterFeature"),
        new ("NServiceBus.INeedInitialization", "RegisterInitializer"),
        new ("NServiceBus.Sagas.IFinder", "RegisterSagaFinder"),
        new ("NServiceBus.IWantToRunBeforeConfigurationIsFinalized", "RegisterConfigurationFinalizer"),
        new ("NServiceBus.IHandleSagaNotFound", "RegisterSagaNotFoundHandler"),
    ];

    static INamedTypeSymbol? GetNamedTypeFromGeneratorSyntaxContext(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var symbol = context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken);

        if (symbol is not INamedTypeSymbol type)
        {
            return null;
        }

        if (type.IsAnonymousType || type.IsImplicitlyDeclared || type.IsComImport || type.IsUnmanagedType || !type.CanBeReferencedByName)
        {
            return null;
        }

        return type;
    }

    record struct ScannedTypeInfo(string DisplayName, string RegisterMethod);
    record struct BuiltInMarkerInfo(string TypeName, string RegisterMethod);
    record struct MarkerTypeInfo(INamedTypeSymbol? Symbol, string RegisterMethod);

    const string ExtensionAttributeMetadataName = "NServiceBus.Extensibility.NServiceBusExtensionPointAttribute";
}