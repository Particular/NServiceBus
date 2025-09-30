#nullable enable
namespace NServiceBus.Core.Analyzer;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
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

        // --- THREE INDEPENDENT MARKER SOURCES ---
        // Each source is kept as a Plural stream until we collect it at the cache boundary.
        // This ensures changes to one marker source don't invalidate caches from other sources.

        // Marker source 1: [NServiceBusExtensionPoint] attributes in user's source code
        var markersFromSourceAttributes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "NServiceBus.Extensibility.NServiceBusExtensionPointAttribute",
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
            .SelectMany((compilation, cancellationToken) =>
            {
                return GetTypesWithExtensionPointAttribute(compilation.GlobalNamespace, cancellationToken);
            });

        // Marker source 3: Built-in marker types (IHandleMessages<T>, Saga<T>, IEvent, ICommand, etc.)
        var builtInMarkers = context.CompilationProvider
            .SelectMany((compilation, cancellationToken) =>
            {
                return MarkerTypeInfos
                    .Select(info => new MarkerTypeInfo(
                        compilation.GetTypeByMetadataName(info.TypeName),
                        info.RegisterMethod))
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
            .SelectMany((pair, cancellationToken) => FindMatchingMarkers(pair.Left, pair.Right));

        var matchedTypesFromCompilationAttributes = sourceTypes
            .Combine(collectedCompilationMarkers)
            .SelectMany((pair, cancellationToken) => FindMatchingMarkers(pair.Left, pair.Right));

        var matchedTypesFromBuiltInMarkers = sourceTypes
            .Combine(collectedBuiltInMarkers)
            .SelectMany((pair, cancellationToken) => FindMatchingMarkers(pair.Left, pair.Right));

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
            .Select((tuple, cancellationToken) =>
            {
                var ((source, compilation), builtIn) = tuple;
                return source.Concat(compilation).Concat(builtIn).ToImmutableArray();
            });

        // --- GENERATE OUTPUT ---
        context.RegisterImplementationSourceOutput(allScannedTypes, static (sourceProductionContext, matches) =>
            GenerateRegistrationCode(sourceProductionContext, matches));
    }

    static IEnumerable<ScannedTypeInfo> FindMatchingMarkers(INamedTypeSymbol? type, ImmutableArray<MarkerTypeInfo> markers)
    {
        if (type is null)
        {
            yield break;
        }

        foreach (var marker in markers)
        {
            if (marker.Symbol is not null && IsAssignableTo(type, marker.Symbol))
            {
                yield return CreateScannedTypeInfo(type, marker);
            }
        }
    }

    static ScannedTypeInfo CreateScannedTypeInfo(INamedTypeSymbol type, MarkerTypeInfo marker)
    {
        string? sagaDataType = null;

        // For sagas, extract the saga data type from Saga<TSagaData>
        if (marker.RegisterMethod == "RegisterSaga")
        {
            var baseType = type.BaseType;
            while (baseType != null)
            {
                if (baseType.OriginalDefinition.ToDisplayString() == "NServiceBus.Saga<TSagaData>")
                {
                    sagaDataType = baseType.TypeArguments[0].ToDisplayString();
                    break;
                }
                baseType = baseType.BaseType;
            }
        }

        return new ScannedTypeInfo(type.ToDisplayString(), marker.RegisterMethod, sagaDataType);
    }

    static void GenerateRegistrationCode(SourceProductionContext sourceProductionContext, ImmutableArray<ScannedTypeInfo> matches)
    {
        // DEBUG: Report matches count
        sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor("NSBGEN001", "Generator Debug", $"Found {matches.Length} types to register", "Debug", DiagnosticSeverity.Info, true),
            Location.None));
            
        if (matches.Length == 0)
        {
            return; // No types to register, don't generate anything
        }

        var sb = new StringBuilder();
        sb.AppendLine("""
                      #nullable enable
                      // <auto-generated/>
                      namespace NServiceBus.Generated
                      {
                          /// <summary>
                          /// Generated type registration for NServiceBus handlers and messages.
                          /// </summary>
                          public static class TypeRegistration
                          {
                              /// <summary>
                              /// Registers all generated types with the endpoint configuration.
                              /// </summary>
                              public static void RegisterTypes(NServiceBus.EndpointConfiguration config)
                              {
                      """);

        foreach (var type in matches)
        {
            if (type.RegisterMethod == "RegisterSaga" && type.SagaDataType != null)
            {
                sb.AppendLine($"            config.{type.RegisterMethod}<{type.DisplayName}, {type.SagaDataType}>();");
            }
            else
            {
                sb.AppendLine($"            config.{type.RegisterMethod}<{type.DisplayName}>();");
            }
        }

        sb.AppendLine("""
                              }
                          }
                      }
                      """);

        sourceProductionContext.AddSource("TypeRegistration.g.cs", sb.ToString());
    }

    static IEnumerable<MarkerTypeInfo> GetTypesWithExtensionPointAttribute(INamespaceSymbol ns, CancellationToken cancellationToken)
    {
        // Walk all types in this namespace and its children looking for [NServiceBusExtensionPoint]
        foreach (var type in ns.GetTypeMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var extensionPointAttr = type.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "NServiceBus.Extensibility.NServiceBusExtensionPointAttribute");
            
            if (extensionPointAttr is not null && extensionPointAttr.ConstructorArguments[0].Value is string registerMethod)
            {
                yield return new MarkerTypeInfo(type, registerMethod);
            }

            // Recursively search nested types
            foreach (var nested in GetNestedTypesWithAttribute(type, cancellationToken))
            {
                yield return nested;
            }
        }

        // Recursively search child namespaces
        foreach (var childNamespace in ns.GetNamespaceMembers())
        {
            foreach (var markerType in GetTypesWithExtensionPointAttribute(childNamespace, cancellationToken))
            {
                yield return markerType;
            }
        }
    }

    static IEnumerable<MarkerTypeInfo> GetNestedTypesWithAttribute(INamedTypeSymbol type, CancellationToken cancellationToken)
    {
        foreach (var nested in type.GetTypeMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var extensionPointAttr = nested.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "NServiceBus.Extensibility.NServiceBusExtensionPointAttribute");
            
            if (extensionPointAttr is not null && extensionPointAttr.ConstructorArguments[0].Value is string registerMethod)
            {
                yield return new MarkerTypeInfo(nested, registerMethod);
            }

            // Recursively search deeper nested types
            foreach (var deeperNested in GetNestedTypesWithAttribute(nested, cancellationToken))
            {
                yield return deeperNested;
            }
        }
    }

    // Customized version that treats IHandleMessages<RealType> the same as IHandleMessages<T>
    static bool IsAssignableTo(INamedTypeSymbol type, INamedTypeSymbol target)
    {
        if (SymbolEqualityComparer.Default.Equals(type, target))
        {
            return true;
        }

        if (target.TypeKind == TypeKind.Interface)
        {
            //return type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, target));
            foreach (var iface in type.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, target))
                {
                    return true;
                }
            }

            return false;
        }

        // walk base types
        var baseType = type.BaseType;
        while (baseType != null)
        {
            if (SymbolEqualityComparer.Default.Equals(baseType, target))
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
        new ("NServiceBus.Installation.INeedToInstallSomething", "RegisterInstaller"),
        new ("NServiceBus.Features.Feature", "RegisterFeature"),
        new ("NServiceBus.INeedInitialization", "RegisterInitializer")
    ];

    record struct BuiltInMarkerInfo(string TypeName, string RegisterMethod);
    
    record struct MarkerTypeInfo(INamedTypeSymbol? Symbol, string RegisterMethod);

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

    record struct ScannedTypeInfo(string DisplayName, string RegisterMethod, string? SagaDataType = null);
}

[Generator]
public sealed class CombinedGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("AssemblyScanningOptionsAttribute.g.cs", SourceText.From(SourceGenerationHelper.FakeStuffForNow, Encoding.UTF8));
        });

        //context.SyntaxProvider.ForAttributeWithMetadataName()

        var sourceTypes = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) => node is TypeDeclarationSyntax,
            transform: GetTypeFromGeneratorSyntaxContext);

        var metadataTypes = context.CompilationProvider
            .SelectMany((compilation, x) => GetAllTypes(compilation.GlobalNamespace))
            .Select(GetTypeData);

        var collectedSource = sourceTypes.Collect();
        var collectedMetadata = metadataTypes.Collect();

        var combined = collectedSource.Combine(collectedMetadata)
            .Select((tuple, cancellationToken) =>
            {
                var (src, meta) = tuple;
                return src.Concat(meta).ToImmutableArray();
            });

        // Instead of RegisterSourceOutput because it doesn't need to be directly called from anything
        // but generated sources - not having it is not going to cause red squigglies
        // https://andrewlock.net/creating-a-source-generator-part-9-avoiding-performance-pitfalls-in-incremental-generators/#7-consider-using-registerimplementationsourceoutput-instead-of-registersourceoutput
        context.RegisterImplementationSourceOutput(combined, (sourceProductionContext, matches) =>
        {
            var sb = new StringBuilder();
            sb.AppendLine("""
                          #nullable enable
                          // <auto-generated/>
                          namespace Generated
                          {
                           public static class Registry
                           {
                               public static readonly string[] Types = new string[]
                               {
                          """);

            foreach (var type in matches)
            {
                if (type is not null)
                {
                    sb.AppendLine($"            \"{type.Value.DisplayName}\",");
                }
            }

            sb.AppendLine("""
                                  };
                              }
                          }
                          """);

            sourceProductionContext.AddSource("Registry.g.cs", sb.ToString());
        });

    }

    static ScannedTypeInfo? GetTypeFromGeneratorSyntaxContext(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var symbol = context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken);
        return GetTypeData(symbol as INamedTypeSymbol, cancellationToken);
    }

    static ScannedTypeInfo? GetTypeData(INamedTypeSymbol? type, CancellationToken cancellationToken)
    {
        if (type is null || type.IsAnonymousType || type.IsImplicitlyDeclared || type.IsComImport || type.IsUnmanagedType || !type.CanBeReferencedByName)
        {
            return null;
        }

        if (!(type.ContainingNamespace?.Name.StartsWith("NServiceBus") ?? false))
        {
            return null;
        }

        return new ScannedTypeInfo(type.ToDisplayString());
    }

    static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol ns)
    {
        foreach (var type in ns.GetTypeMembers())
        {
            yield return type;

            foreach (var nested in GetAllTypes(type))
            {
                yield return nested;
            }
        }

        foreach (var nestedNs in ns.GetNamespaceMembers())
        {
            foreach (var type in GetAllTypes(nestedNs))
            {
                yield return type;
            }
        }
    }

    static IEnumerable<INamedTypeSymbol> GetAllTypes(INamedTypeSymbol type)
    {
        foreach (var nested in type.GetTypeMembers())
        {
            yield return nested;
            foreach (var t in GetAllTypes(nested))
            {
                yield return t;
            }
        }
    }

    record struct ScannedTypeInfo(string DisplayName);
}

public class SourceGenerationHelper
{
    public const string FakeStuffForNow = "";
}

//using System.Text;
//using System.Threading;
//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
//using Microsoft.CodeAnalysis.Text;

//[Generator]
//public sealed class KnownTypesGenerator : IIncrementalGenerator
//{
//    public void Initialize(IncrementalGeneratorInitializationContext context)
//    {


//        var builtInMarkerTypes = context.CompilationProvider.Select((compilation, cancellationToken) =>
//        {
//            return MarkerTypeInfos
//                .Select(info => compilation.GetTypeByMetadataName(info.TypeName))
//                .ToImmutableArray();
//        });

//        var markerTypes = builtInMarkerTypes;

//        var sourceTypes = context.SyntaxProvider
//            .CreateSyntaxProvider(
//                predicate: static (node, _) => node is TypeDeclarationSyntax,
//                transform: GetNamedTypeFromGeneratorSyntaxContext)
//            .Where(type => type is not null);

//        // Check for types marked with [NsbHandler] attribute
//        var nsbAttibuteTypes = context.SyntaxProvider
//            .ForAttributeWithMetadataName(
//                "NServiceBus.Extensibility.NServiceBusExtentionPointAttribute",
//                predicate: static (node, _) => node is TypeDeclarationSyntax,
//                transform: GetNamedTypeFromAttributeContext)
//            .Where(type => type is not null)
//            .Select((type, cancellationToken) => new ScannedTypeInfo(type!.ToDisplayString()));

//        var relevantTypes = sourceTypes.Combine(markerTypes)
//            .Where(pair =>
//            {
//                var (type, markerTypeSymbols) = pair;
//                return markerTypeSymbols.Any(markerType => IsAssignableTo(type!, markerType!));
//            })
//            .Where(pair => pair.Left is not null)
//            .Select((pair, cancellationToken) => new ScannedTypeInfo(pair.Left!.ToDisplayString()));

//        var collected = relevantTypes.Collect();

//        //// Collect all types separately, then combine
//        //var collectedRelevant = relevantTypes.Collect();
//        //var collectedNsbHandler = nsbAttibuteTypes.Collect();

//        //var combined = collectedRelevant
//        //    .Combine(collectedNsbHandler)
//        //    .Select((tuple, cancellationToken) =>
//        //    {
//        //        var (((relevant, nsbHandler), nsbSaga), nsbFeature) = tuple;
//        //        return relevant.Concat(nsbHandler).Concat(nsbSaga).Concat(nsbFeature).ToImmutableArray();
//        //    });

//        // Instead of RegisterSourceOutput because it doesn't need to be directly called from anything
//        // but generated sources - not having it is not going to cause red squigglies
//        // https://andrewlock.net/creating-a-source-generator-part-9-avoiding-performance-pitfalls-in-incremental-generators/#7-consider-using-registerimplementationsourceoutput-instead-of-registersourceoutput
//        context.RegisterImplementationSourceOutput(combined, (sourceProductionContext, matches) =>
//        {
//            var sb = new StringBuilder();
//            sb.AppendLine("""
//                          #nullable enable
//                          // <auto-generated/>
//                          namespace Generated
//                          {
//                              public static class Registry
//                              {
//                                  public static readonly System.Type[] Types = new System.Type[]
//                                  {
//                          """);

//            foreach (var type in matches)
//            {
//                sb.AppendLine($"            typeof({type.DisplayName}),");
//            }

//            sb.AppendLine("""
//                                  };
//                              }
//                          }
//                          """);

//            sourceProductionContext.AddSource("Registry.g.cs", sb.ToString());
//        });
//    }

//    // Customized version that treats IHandleMessages<RealType> the same as IHandleMessages<T>
//    static bool IsAssignableTo(INamedTypeSymbol type, INamedTypeSymbol target)
//    {
//        if (SymbolEqualityComparer.Default.Equals(type, target))
//        {
//            return true;
//        }

//        if (target.TypeKind == TypeKind.Interface)
//        {
//            //return type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, target));
//            foreach (var iface in type.AllInterfaces)
//            {
//                if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, target))
//                {
//                    return true;
//                }
//            }

//            return false;
//        }

//        // walk base types
//        var baseType = type.BaseType;
//        while (baseType != null)
//        {
//            if (SymbolEqualityComparer.Default.Equals(baseType, target))
//            {
//                return true;
//            }

//            baseType = baseType.BaseType;
//        }

//        return false;
//    }

//    static readonly ImmutableArray<MarkerTypeInfo> MarkerTypeInfos =
//    [
//        new ("NServiceBus.IEvent", "AddEvent"),
//        new ("NServiceBus.ICommand", "AddCommand"),
//        new ("NServiceBus.IMessage", "AddMessage"),
//        new ("NServiceBus.IHandleMessages`1", "AddHandler"),
//        new ("NServiceBus.Installation.INeedToInstallSomething", "AddInstaller"),
//    ];

//    public record struct MarkerTypeInfo(string TypeName, string RegisterMethod);

//    static INamedTypeSymbol? GetNamedTypeFromGeneratorSyntaxContext(GeneratorSyntaxContext context, CancellationToken cancellationToken)
//    {
//        var symbol = context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken);

//        if (symbol is not INamedTypeSymbol type)
//        {
//            return null;
//        }

//        if (type.IsAnonymousType || type.IsImplicitlyDeclared || type.IsComImport || type.IsUnmanagedType || !type.CanBeReferencedByName)
//        {
//            return null;
//        }

//        return type;
//    }

//    static INamedTypeSymbol? GetNamedTypeFromAttributeContext(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
//    {
//        var symbol = context.SemanticModel.GetDeclaredSymbol(context.TargetNode, cancellationToken);

//        if (symbol is not INamedTypeSymbol type)
//        {
//            return null;
//        }

//        if (type.IsAnonymousType || type.IsImplicitlyDeclared || type.IsComImport || type.IsUnmanagedType || !type.CanBeReferencedByName)
//        {
//            return null;
//        }

//        return type;
//    }

//    record struct ScannedTypeInfo(string DisplayName);
//}

//[Generator]
//public sealed class CombinedGenerator : IIncrementalGenerator
//{
//    public void Initialize(IncrementalGeneratorInitializationContext context)
//    {
//        context.RegisterPostInitializationOutput(ctx =>
//        {
//            ctx.AddSource("AssemblyScanningOptionsAttribute.g.cs", SourceText.From(SourceGenerationHelper.FakeStuffForNow, Encoding.UTF8));
//        });

//        //context.SyntaxProvider.ForAttributeWithMetadataName()

//        var sourceTypes = context.SyntaxProvider.CreateSyntaxProvider(
//            predicate: static (node, _) => node is TypeDeclarationSyntax,
//            transform: GetTypeFromGeneratorSyntaxContext);

//        var metadataTypes = context.CompilationProvider
//            .SelectMany((compilation, x) => GetAllTypes(compilation.GlobalNamespace))
//            .Select(GetTypeData);

//        var collectedSource = sourceTypes.Collect();
//        var collectedMetadata = metadataTypes.Collect();

//        var combined = collectedSource.Combine(collectedMetadata)
//            .Select((tuple, cancellationToken) =>
//            {
//                var (src, meta) = tuple;
//                return src.Concat(meta).ToImmutableArray();
//            });

//        // Instead of RegisterSourceOutput because it doesn't need to be directly called from anything
//        // but generated sources - not having it is not going to cause red squigglies
//        // https://andrewlock.net/creating-a-source-generator-part-9-avoiding-performance-pitfalls-in-incremental-generators/#7-consider-using-registerimplementationsourceoutput-instead-of-registersourceoutput
//        context.RegisterImplementationSourceOutput(combined, (sourceProductionContext, matches) =>
//        {
//            var sb = new StringBuilder();
//            sb.AppendLine("""
//                          #nullable enable
//                          // <auto-generated/>
//                          namespace Generated
//                          {
//                           public static class Registry
//                           {
//                               public static readonly string[] Types = new string[]
//                               {
//                          """);

//            foreach (var type in matches)
//            {
//                if (type is not null)
//                {
//                    sb.AppendLine($"            \"{type.Value.DisplayName}\",");
//                }
//            }

//            sb.AppendLine("""
//                                  };
//                              }
//                          }
//                          """);

//            sourceProductionContext.AddSource("Registry.g.cs", sb.ToString());
//        });

//    }

//    static ScannedTypeInfo? GetTypeFromGeneratorSyntaxContext(GeneratorSyntaxContext context, CancellationToken cancellationToken)
//    {
//        var symbol = context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken);
//        return GetTypeData(symbol as INamedTypeSymbol, cancellationToken);
//    }

//    static ScannedTypeInfo? GetTypeData(INamedTypeSymbol? type, CancellationToken cancellationToken)
//    {
//        if (type is null || type.IsAnonymousType || type.IsImplicitlyDeclared || type.IsComImport || type.IsUnmanagedType || !type.CanBeReferencedByName)
//        {
//            return null;
//        }

//        if (!(type.ContainingNamespace?.Name.StartsWith("NServiceBus") ?? false))
//        {
//            return null;
//        }

//        return new ScannedTypeInfo(type.ToDisplayString());
//    }

//    static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol ns)
//    {
//        foreach (var type in ns.GetTypeMembers())
//        {
//            yield return type;

//            foreach (var nested in GetAllTypes(type))
//            {
//                yield return nested;
//            }
//        }

//        foreach (var nestedNs in ns.GetNamespaceMembers())
//        {
//            foreach (var type in GetAllTypes(nestedNs))
//            {
//                yield return type;
//            }
//        }
//    }

//    static IEnumerable<INamedTypeSymbol> GetAllTypes(INamedTypeSymbol type)
//    {
//        foreach (var nested in type.GetTypeMembers())
//        {
//            yield return nested;
//            foreach (var t in GetAllTypes(nested))
//            {
//                yield return t;
//            }
//        }
//    }

//    record struct ScannedTypeInfo(string DisplayName);
//}

//public class SourceGenerationHelper
//{
//    public const string FakeStuffForNow = "";
//}