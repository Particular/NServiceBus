#nullable enable

namespace NServiceBus.Core.Analyzer;

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class HandlerRegistryAvailabilityAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [NoHandlerRegistryFound];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        // Contributors are usually emitted by generators, so generated code counts for detecting a registry. Diagnostics in generated code are not reported.
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var compilation = compilationContext.Compilation;
            if (compilation.GetTypeByMetadataName("NServiceBus.HandlerRegistry") is null)
            {
                return;
            }

            var handlerAttribute = compilation.GetTypeByMetadataName("NServiceBus.HandlerAttribute");
            var sagaAttribute = compilation.GetTypeByMetadataName("NServiceBus.SagaAttribute");
            var contributorAttribute = compilation.GetTypeByMetadataName("NServiceBus.HandlerRegistryContributorAttribute");
            var contributorInterface = compilation.GetTypeByMetadataName(AddHandlerAndSagasRegistrationGenerator.Parser.ContributorInterfaceMetadataName);

            var calls = new ConcurrentBag<(Location Location, string MethodName)>();
            var participates = HandlerRegistryCompositionOptions.Participates(compilationContext.Options.AnalyzerConfigOptionsProvider.GlobalOptions);
            var hasLocalRegistry = false;

            compilationContext.RegisterOperationAction(context =>
            {
                if (context.Operation is IInvocationOperation { TargetMethod: { } method } invocation &&
                    HandlerRegistryCompositionInterceptor.Parser.IsCompositionMethod(method))
                {
                    calls.Add((invocation.Syntax.GetLocation(), method.Name));
                }
            }, OperationKind.Invocation);

            compilationContext.RegisterSymbolAction(context =>
            {
                if (!participates || hasLocalRegistry || context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class } type)
                {
                    return;
                }

                if (IsRegistered(type, handlerAttribute, sagaAttribute, contributorAttribute, contributorInterface, context.Compilation))
                {
                    hasLocalRegistry = true;
                }
            }, SymbolKind.NamedType);

            compilationContext.RegisterCompilationEndAction(context =>
            {
                if (calls.IsEmpty || hasLocalRegistry ||
                    HandlerRegistryCompositionInterceptor.Parser.DiscoverReferencedEntries(context.Compilation, context.CancellationToken).Count > 0)
                {
                    return;
                }

                foreach (var (location, methodName) in calls)
                {
                    context.ReportDiagnostic(Diagnostic.Create(NoHandlerRegistryFound, location, methodName));
                }
            });
        });
    }

    static bool IsRegistered(INamedTypeSymbol type, INamedTypeSymbol? handlerAttribute, INamedTypeSymbol? sagaAttribute, INamedTypeSymbol? contributorAttribute, INamedTypeSymbol? contributorInterface, Compilation compilation)
    {
        if (type.IsAbstract)
        {
            return false;
        }

        foreach (var attribute in type.GetAttributes())
        {
            var attributeClass = attribute.AttributeClass;
            if (SymbolEqualityComparer.Default.Equals(attributeClass, handlerAttribute) || SymbolEqualityComparer.Default.Equals(attributeClass, sagaAttribute))
            {
                return true;
            }

            if (contributorInterface is not null && SymbolEqualityComparer.Default.Equals(attributeClass, contributorAttribute) &&
                AddHandlerAndSagasRegistrationGenerator.Parser.GetContributorProblems(type, contributorInterface, compilation).Count == 0)
            {
                return true;
            }
        }

        return false;
    }

    static readonly DiagnosticDescriptor NoHandlerRegistryFound = new(
        id: DiagnosticIds.NoHandlerRegistryFound,
        title: "No handler registry found",
        messageFormat: "HandlerRegistry.{0} registers nothing because neither this assembly nor a referenced assembly has a generated handler registry.",
        category: "Code",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);
}
