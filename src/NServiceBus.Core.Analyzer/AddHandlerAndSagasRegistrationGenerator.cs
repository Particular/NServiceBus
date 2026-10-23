#nullable enable

namespace NServiceBus.Core.Analyzer;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Utility;

[Generator(LanguageNames.CSharp)]
public partial class AddHandlerAndSagasRegistrationGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var addHandlers = context.SyntaxProvider
            .ForAttributeWithMetadataName("NServiceBus.HandlerAttribute",
                predicate: static (node, _) => IsHandlerOrSagaCandidate(node),
                transform: static (context, cancellationToken) => Parser.Parse(context, Parser.SpecKind.Handler, cancellationToken: cancellationToken))
            .Where(static spec => spec is not null)
            .Select(static (spec, _) => spec!)
            .WithTrackingName(TrackingNames.HandlerSpecs);

        var addSagas = context.SyntaxProvider
            .ForAttributeWithMetadataName("NServiceBus.SagaAttribute",
                predicate: static (node, _) => IsHandlerOrSagaCandidate(node),
                transform: static (context, cancellationToken) => Parser.Parse(context, Parser.SpecKind.Saga, cancellationToken: cancellationToken))
            .Where(static spec => spec is not null)
            .Select(static (spec, _) => spec!)
            .WithTrackingName(TrackingNames.SagaSpecs);

        var contributors = BuildContributorSpecsPipeline(context);

        var assemblyName = context.CompilationProvider
            .Select(static (compilation, _) => compilation.Assembly.Name)
            .WithTrackingName(TrackingNames.AssemblyName);

        var collected = addHandlers.Collect()
            .Combine(addSagas.Collect())
            .Select((pair, _) => pair.Left.AddRange(pair.Right))
            .WithTrackingName(TrackingNames.HandlerAndSagaSpecs);

        var rootTypeSpec = BuildRootTypeSpecPipeline(context);

        var combined = collected.Combine(rootTypeSpec).Combine(contributors).Combine(assemblyName);

        context.RegisterSourceOutput(combined,
            static (productionContext, spec) =>
            {
                var emitter = new Emitter(productionContext);
                emitter.Emit(spec.Left.Left.Left, spec.Left.Right, spec.Left.Left.Right, spec.Right);
            });
    }

    internal static bool IsHandlerOrSagaCandidate(SyntaxNode node) =>
        node is ClassDeclarationSyntax classDeclarationSyntax && !classDeclarationSyntax.Modifiers.Any(SyntaxKind.AbstractKeyword);

    internal static IncrementalValueProvider<bool> BuildHasRegistryPipeline(IncrementalGeneratorInitializationContext context)
    {
        var hasHandlers = BuildHasAttributedClassPipeline(context, "NServiceBus.HandlerAttribute");
        var hasSagas = BuildHasAttributedClassPipeline(context, "NServiceBus.SagaAttribute");
        var hasContributors = BuildContributorSpecsPipeline(context)
            .Select(static (contributors, _) => contributors.Count > 0);

        return hasHandlers.Combine(hasSagas).Combine(hasContributors)
            .Select(static (flags, _) => flags.Left.Left || flags.Left.Right || flags.Right);
    }

    static IncrementalValueProvider<bool> BuildHasAttributedClassPipeline(IncrementalGeneratorInitializationContext context, string attributeMetadataName) =>
        context.SyntaxProvider
            .ForAttributeWithMetadataName(attributeMetadataName,
                predicate: static (node, _) => IsHandlerOrSagaCandidate(node),
                transform: static (_, _) => true)
            .Collect()
            .Select(static (matches, _) => matches.Length > 0);

    internal static IncrementalValueProvider<ImmutableEquatableArray<Parser.ContributorSpec>> BuildContributorSpecsPipeline(IncrementalGeneratorInitializationContext context) =>
        context.SyntaxProvider
            .ForAttributeWithMetadataName("NServiceBus.HandlerRegistryContributorAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
                transform: static (context, cancellationToken) => Parser.ParseContributor(context, cancellationToken))
            .Where(static spec => spec.HasValue)
            .Select(static (spec, _) => spec!.Value)
            .WithTrackingName(TrackingNames.ContributorSpec)
            .Collect()
            .Select(static (contributors, _) => Parser.SelectContributors(contributors))
            .WithTrackingName(TrackingNames.ContributorSpecs);

    internal static IncrementalValueProvider<Parser.RootTypeSpec> BuildRootTypeSpecPipeline(IncrementalGeneratorInitializationContext context)
    {
        var assemblyInfo = context.CompilationProvider
            .Select(static (compilation, _) =>
            {
                var assemblyName = compilation.AssemblyName ?? string.Empty;
                var assemblyId = Emitter.SanitizeIdentifier(assemblyName);
                return (AssemblyName: assemblyName, AssemblyId: assemblyId);
            })
            .WithTrackingName(TrackingNames.AssemblyInfo);

        var explicitRootTypeSpec = context.SyntaxProvider
            .ForAttributeWithMetadataName("NServiceBus.HandlerRegistryExtensionsAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: Parser.ParseRootTypeSpec)
            .Where(static spec => spec.HasValue)
            .Select(static (spec, _) => spec!.Value)
            .Collect()
            .WithTrackingName(TrackingNames.ExplicitRootTypeSpec);

        return explicitRootTypeSpec
            .Combine(assemblyInfo)
            .Select(static (pair, _) => Parser.SelectRootTypeSpec(pair.Left, pair.Right.AssemblyId))
            .WithTrackingName(TrackingNames.RootTypeSpec);
    }
}