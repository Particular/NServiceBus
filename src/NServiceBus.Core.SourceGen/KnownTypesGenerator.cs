namespace NServiceBus.Core.SourceGen;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Analyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

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

        // Get info about compilation to control the name of the outputted class, so that it does
        // not conflict with generated classes from other projects in the solution
        var assemblyNameInfo = context.CompilationProvider
            .Select((compilation, _) => CompilationAssemblyDetails.FromAssembly(compilation.Assembly))
            .Select((assemblyDetails, _) => assemblyDetails.ToGenerationClassName());

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
                        attributeInfo.ConstructorArguments[0].Value is string registrationMethodName &&
                        attributeInfo.ConstructorArguments[1].Value is bool autoRegister)
                    {
                        var markerInfo = new MarkerInfo(decoratedType.ToDisplayString(), registrationMethodName, autoRegister);
                        if (decoratedType.IsGenericType)
                        {
                            var syntaxRef = attributeInfo.ApplicationSyntaxReference;
                            var location = new LocationProxy(syntaxRef!.SyntaxTree, syntaxRef.Span);
                            var diagnosticInfo = new DiagnosticInfo(ExtensionTypeCantBeGeneric, location, markerInfo.TypeName);
                            return new MarkerTypeInfo(decoratedType, markerInfo, diagnosticInfo);
                        }
                        return new MarkerTypeInfo(decoratedType, markerInfo);
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
                    .Select(info =>
                    {
                        // Throw here if symbols are not found or not right, since they're internal to NServiceBus Core
                        var symbol = compilation.GetTypeByMetadataName(info.TypeName);
                        if (symbol is null)
                        {
                            throw new InvalidOperationException($"Symbol for NServiceBus known marker type '{info.TypeName}' could not be found in the compilation.");
                        }
                        if (symbol.IsGenericType)
                        {
                            throw new InvalidOperationException($"Marker type '{info.TypeName}' cannot be an unbound generic type.");
                        }
                        return new MarkerTypeInfo(symbol, new MarkerInfo(info.TypeName, info.RegisterMethod, info.AutoRegister));
                    });
            });

        // --- CACHE BOUNDARY: Collect each marker source independently ---
        // Collecting here creates separate cache entries for each marker source.
        // Changes to source attributes don't invalidate compilation or built-in marker caches.
        // However, can't test on markers here because MarkerTypeInfo includes INamedTypeSymbol which is not cacheable.
        var collectedSourceMarkers = markersFromSourceAttributes
            .Where(m => m.Diagnostic is null)
            .Collect();
        var collectedCompilationMarkers = markersFromCompilationAttributes
            .Where(m => m.Diagnostic is null)
            .Collect();
        var collectedBuiltInMarkers = builtInMarkers
            .Where(m => m.Diagnostic is null)
            .Collect();

        // Plus any markers that have diagnostics attached. These should only come from source, because built-in markers should be valid or throw,
        // and markers from the compilation are stuck in their compiled state - can't raise a diagnostic for those.
        var diagnostics = markersFromSourceAttributes
            .Where(m => m.Diagnostic != null)
            .Select((m, _) => m.Diagnostic)
            .Collect();

        // --- MATCH TYPES AGAINST MARKERS ---
        // For each source type (Plural), pair it with each marker array (Singular).
        // The Combine API requires: Combine<Plural, Singular>, so we collected markers above.
        // Result: When a new type is added, it flows through as ONE item and gets checked
        // against the cached marker arrays - very efficient!
        var matchedTypesFromSourceAttributes = sourceTypes
            .Combine(collectedSourceMarkers)
            .SelectMany((pair, cancellationToken) => FindMatchingMarkers(pair.Left, pair.Right, cancellationToken))
            .WithTrackingName("TypesFromSource");

        var matchedTypesFromCompilationAttributes = sourceTypes
            .Combine(collectedCompilationMarkers)
            .SelectMany((pair, cancellationToken) => FindMatchingMarkers(pair.Left, pair.Right, cancellationToken))
            .WithTrackingName("TypesFromCompilation");

        var matchedTypesFromBuiltInMarkers = sourceTypes
            .Combine(collectedBuiltInMarkers)
            .SelectMany((pair, cancellationToken) => FindMatchingMarkers(pair.Left, pair.Right, cancellationToken))
            .WithTrackingName("TypesFromBuiltIn");

        // --- CACHE BOUNDARY: Collect matched types from each source independently ---
        // Final collection before combining results - maintains separation between sources
        var collectedFromSource = matchedTypesFromSourceAttributes
            .Collect().WithTrackingName("CollectedFromSource");
        var collectedFromCompilation = matchedTypesFromCompilationAttributes
            .Collect().WithTrackingName("CollectedFromCompilation");
        var collectedFromBuiltIn = matchedTypesFromBuiltInMarkers
            .Collect().WithTrackingName("CollectedFromBuiltIn");

        // --- FINAL COMBINATION ---
        // Only now do we merge all three result sets into one array for source generation
        var generationData = collectedFromSource
            .Combine(collectedFromCompilation)
            .Combine(collectedFromBuiltIn)
            .Combine(assemblyNameInfo)
            .Combine(diagnostics)
            .Select((tuple, _) =>
            {
                var ((((source, compilation), builtIn), assemblyName), diagnosticInfos) = tuple;
                var allScannedTypes = source.Concat(compilation).Concat(builtIn)
                    // CompilationProvider and SyntaxProvider can both surface an extension point type in user code
                    .Distinct()
                    .ToImmutableArray();

                return new GenerationData(assemblyName, allScannedTypes, diagnosticInfos);
            });

        // --- GENERATE OUTPUT ---
        context.RegisterImplementationSourceOutput(generationData, GenerateRegistrationCode);
    }

    record struct GenerationData(string AssemblyName, ImmutableArray<ScannedTypeInfo> Types, ImmutableArray<DiagnosticInfo?> Diagnostics);

    static IEnumerable<ScannedTypeInfo> FindMatchingMarkers(INamedTypeSymbol? type, ImmutableArray<MarkerTypeInfo> markers, CancellationToken cancellationToken)
    {
        if (type is null || markers.Length == 0)
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
                yield return new ScannedTypeInfo(type.ToDisplayString(), marker.Marker);
            }
        }
    }

    /// <auto-generated />
    static void GenerateRegistrationCode(SourceProductionContext sourceProductionContext, GenerationData data)
    {
        foreach (var info in data.Diagnostics)
        {
            if (info.HasValue)
            {
                sourceProductionContext.ReportDiagnostic(info.Value.CreateDiagnostic());
            }
        }

        if (data.Types.Length == 0)
        {
            return; // No types to register, don't generate anything
        }

        var sb = new StringBuilder();
        sb.AppendLine($$"""
                      // <auto-generated/>
                      #nullable enable
                      namespace NServiceBus.Generated
                      {
                          /// <summary>
                          /// Source-generated registrations for types normally discovered by assembly scanning.
                          /// </summary>
                          [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                          [NServiceBus.Extensibility.AutoGeneratedTypeRegistrationsAttribute]
                          public static class {{data.AssemblyName}}
                          {
                              /// <summary>
                              /// Registers types that are always discovered by assembly scanning.
                              /// </summary>
                              public static void RegisterRequiredTypes(NServiceBus.EndpointConfiguration config)
                              {
                      """);

        foreach (var type in data.Types.Where(t => t.Marker.AutoRegister))
        {
            sb.AppendLine($"            config.TypeRegistrations.RegisterExtensionType<{type.Marker.TypeName}, {type.DisplayName}>();");
        }

        sb.AppendLine("""
                              }
                      
                              /// <summary>
                              /// Registers handlers/sagas that are not required to be present, allowing them to
                              /// be registered manually.
                              /// </summary>
                              public static void RegisterOptionalTypes(NServiceBus.EndpointConfiguration config)
                              {
                      """);

        foreach (var type in data.Types.Where(t => !t.Marker.AutoRegister))
        {
            sb.AppendLine($"            config.TypeRegistrations.RegisterExtensionType<{type.Marker.TypeName}, {type.DisplayName}>();");
        }

        sb.AppendLine("""
                              }
                          }

                          // public static class TemporaryRegistrationExtensions
                          // {
                      """);
        foreach (var marker in data.Types.Select(m => m.Marker).Distinct())
        {
            sb.AppendLine($"    //    public static void {marker.RegisterMethod}<TType>(this NServiceBus.EndpointConfiguration config) where TType : {marker.TypeName} {{ }}");
        }
        sb.AppendLine("""
                          // }
                      }
                      """);

        sourceProductionContext.AddSource("TypeRegistration.g.cs", sb.ToString());
    }

    static readonly DiagnosticDescriptor ExtensionTypeCantBeGeneric = new DiagnosticDescriptor(
        id: DiagnosticIds.ExtensionTypeAttributeCantDecorateGenericType,
        title: "Extension types cannot be generic",
        messageFormat: "Type '{0}' cannot be identified as an NServiceBus extension type because it is a generic type",
        category: "Source Generation",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    static IEnumerable<MarkerTypeInfo> GetTypesWithExtensionPointAttribute(Compilation compilation, CancellationToken cancellationToken)
    {
        var attributeType = compilation.GetTypeByMetadataName(ExtensionAttributeMetadataName);
        if (attributeType is null)
        {
            return [];
        }

        return GetTypesWithExtensionPointAttribute(compilation.GlobalNamespace, attributeType, compilation.Assembly, cancellationToken);
    }

    static IEnumerable<MarkerTypeInfo> GetTypesWithExtensionPointAttribute(INamespaceSymbol ns, INamedTypeSymbol attributeType, IAssemblySymbol compilationAssembly, CancellationToken cancellationToken)
    {
        if (SymbolEqualityComparer.Default.Equals(ns.ContainingAssembly, compilationAssembly))
        {
            yield break;
        }

        // Walk all types in this namespace and its children looking for [NServiceBusExtensionPoint]
        foreach (var type in ns.GetTypeMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var extensionPointAttr = type.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeType));

            if (extensionPointAttr is not null && extensionPointAttr.ConstructorArguments[0].Value is string registerMethod &&
                extensionPointAttr.ConstructorArguments[1].Value is bool autoRegister)
            {
                //if (!type.IsGenericType)
                //{
                yield return new MarkerTypeInfo(type, new(type.ToDisplayString(), registerMethod, autoRegister));
                //}
            }

            // Recursively search nested types
            foreach (var nested in GetNestedTypesWithAttribute(type, attributeType, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return nested;
            }
        }

        // Recursively search child namespaces
        foreach (var childNamespace in ns.GetNamespaceMembers())
        {
            foreach (var markerType in GetTypesWithExtensionPointAttribute(childNamespace, attributeType, compilationAssembly, cancellationToken))
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

            if (extensionPointAttr is not null && extensionPointAttr.ConstructorArguments[0].Value is string registerMethod &&
                extensionPointAttr.ConstructorArguments[1].Value is bool autoRegister)
            {
                yield return new MarkerTypeInfo(nested, new(nested.ToDisplayString(), registerMethod, autoRegister));
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

        if (type.IsAbstract)
        {
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
    static readonly ImmutableArray<MarkerInfo> MarkerTypeInfos =
    [
        // Not sure we need these at all
        new ("NServiceBus.IEvent", "RegisterEvent", true),
        new ("NServiceBus.ICommand", "RegisterCommand", true),
        new ("NServiceBus.IMessage", "RegisterMessage", true),
        new ("NServiceBus.IContainSagaData", "RegisterSagaData", false),

        // Obvious per-endpoint registration choices
        new ("NServiceBus.IHandleMessages", "RegisterHandler", false),
        new ("NServiceBus.Saga", "RegisterSaga", false),
        new ("NServiceBus.Sagas.IFinder", "RegisterSagaFinder", true),
        new ("NServiceBus.Sagas.IHandleSagaNotFound", "RegisterSagaNotFoundHandler", true),

        // May be able to remove features as an explicit-registration only
        new ("NServiceBus.Installation.INeedToInstallSomething", "RegisterInstaller", true),

        // Obsolete?
        new ("NServiceBus.INeedInitialization", "RegisterInitializer", true),
        new ("NServiceBus.IWantToRunBeforeConfigurationIsFinalized", "RegisterConfigurationFinalizer", true),

        // Custom checks are out there ;-)

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

    record struct ScannedTypeInfo(string DisplayName, MarkerInfo Marker);
    record struct MarkerInfo(string TypeName, string RegisterMethod, bool AutoRegister);
    record struct MarkerTypeInfo(INamedTypeSymbol? Symbol, MarkerInfo Marker, DiagnosticInfo? Diagnostic = null);

    record struct DiagnosticInfo(DiagnosticDescriptor Descriptor, LocationProxy Location, params string[] MessageArgs)
    {
        public Diagnostic CreateDiagnostic() => Diagnostic.Create(Descriptor, Location.ToLocation(),
            messageArgs: MessageArgs.ToArray<object>());
    }

    record struct LocationProxy(SyntaxTree Tree, TextSpan Span)
    {
        public Location ToLocation() => Location.Create(Tree, Span);
    }

    const string ExtensionAttributeMetadataName = "NServiceBus.Extensibility.NServiceBusExtensionPointAttribute";
}