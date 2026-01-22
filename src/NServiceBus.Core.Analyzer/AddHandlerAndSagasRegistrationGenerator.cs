#nullable enable

namespace NServiceBus.Core.Analyzer;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[Generator(LanguageNames.CSharp)]
public partial class AddHandlerAndSagasRegistrationGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var addHandlers = context.SyntaxProvider
            .ForAttributeWithMetadataName("NServiceBus.HandlerAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax classDeclarationSyntax && !classDeclarationSyntax.Modifiers.Any(SyntaxKind.AbstractKeyword),
                transform: Parser.Parse)
            .Where(static spec => spec is not null)
            .Select(static (spec, _) => spec!)
            .WithTrackingName("HandlerSpecs");

        var addSagas = context.SyntaxProvider
            .ForAttributeWithMetadataName("NServiceBus.SagaAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax classDeclarationSyntax && !classDeclarationSyntax.Modifiers.Any(SyntaxKind.AbstractKeyword),
                transform: Parser.Parse)
            .Where(static spec => spec is not null)
            .Select(static (spec, _) => spec!)
            .WithTrackingName("SagaSpecs");

        var collected = addHandlers.Collect()
            .Combine(addSagas.Collect())
            .Select((pair, _) => pair.Left.AddRange(pair.Right))
            .WithTrackingName("HandlerAndSagaSpecs");

        var assemblyInfo = context.CompilationProvider
            .Select(static (compilation, _) =>
            {
                var assemblyName = compilation.AssemblyName ?? string.Empty;
                var assemblyId = Emitter.SanitizeIdentifier(assemblyName);
                return (AssemblyName: assemblyName, AssemblyId: assemblyId);
            })
            .WithTrackingName("AssemblyInfo");

        var explicitRootTypeSpec = context.SyntaxProvider
            .ForAttributeWithMetadataName("NServiceBus.HandlerRegistryExtensionsAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: Parser.ParseRootTypeSpec)
            .Where(static spec => spec.HasValue)
            .Select(static (spec, _) => spec!.Value)
            .Collect()
            .WithTrackingName("ExplicitRootTypeSpec");

        var rootTypeSpec = explicitRootTypeSpec
            .Combine(assemblyInfo)
            .Select(static (pair, _) => Parser.SelectRootTypeSpec(pair.Left, pair.Right.AssemblyId))
            .WithTrackingName("RootTypeSpec");

        var combined = collected.Combine(rootTypeSpec);

        context.RegisterSourceOutput(combined,
            static (productionContext, spec) =>
            {
                var emitter = new Emitter(productionContext);
                emitter.Emit(spec.Left, spec.Right);
            });
    }
}