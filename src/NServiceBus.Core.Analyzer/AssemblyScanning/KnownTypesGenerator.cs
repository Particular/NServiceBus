#nullable enable
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

        // Get compilation info about built-in marker types (IHandleMessages<T>, Saga<T>,etc.)
        // Keep as Plural ValuesProvider until we collect it at the cache boundary
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

        // --- CACHE BOUNDARY: Collect the markers, however, can't test on markers here
        // because MarkerTypeInfo includes INamedTypeSymbol which is not cacheable.
        var collectedMarkers = builtInMarkers
            .Collect();

        // Match source types against markers
        // The Combine API requires: Combine<Plural, Singular>, so we collected markers above.
        // Result: When a new type is added, it flows through as ONE item and gets checked
        // against the cached marker arrays - very efficient!
        var matchedTypes = sourceTypes
            .Combine(collectedMarkers)
            .SelectMany((pair, cancellationToken) => FindMatchingMarkers(pair.Left, pair.Right, cancellationToken))
            .WithTrackingName("MatchedTypes");

        // --- CACHE BOUNDARY: Collect matched types
        // Final collection before combining results - maintains separation between sources
        var collectedTypes = matchedTypes
            .Collect().WithTrackingName("CollectedTypes");

        // --- FINAL COMBINATION ---
        // Only now do we merge all three result sets into one array for source generation
        var generationData = collectedTypes
            .Combine(assemblyNameInfo)
            .Select((tuple, _) =>
            {
                var (builtIn, assemblyName) = tuple;
                var allScannedTypes = builtIn
                    // CompilationProvider and SyntaxProvider can both surface an extension point type in user code
                    .Distinct()
                    .ToImmutableArray();

                return new GenerationData(assemblyName, allScannedTypes);
            });

        // --- GENERATE OUTPUT ---
        context.RegisterImplementationSourceOutput(generationData, GenerateRegistrationCode);
    }

    record struct GenerationData(string AssemblyName, ImmutableArray<ScannedTypeInfo> Types);

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
    record struct MarkerTypeInfo(INamedTypeSymbol? Symbol, MarkerInfo Marker);
}