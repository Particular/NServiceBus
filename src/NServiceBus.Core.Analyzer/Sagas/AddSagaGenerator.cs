#nullable enable

namespace NServiceBus.Core.Analyzer.Sagas;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NServiceBus.Core.Analyzer;

[Generator(LanguageNames.CSharp)]
public sealed partial class AddSagaGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var knownTypes = KnownTypePipelines.BuildHandlerKnownTypesPipeline(context);

        var addSagas = context.SyntaxProvider
            .ForAttributeWithMetadataName("NServiceBus.SagaAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax classDeclarationSyntax && !classDeclarationSyntax.Modifiers.Any(SyntaxKind.AbstractKeyword),
                transform: static (ctx, _) => (sagaType: (INamedTypeSymbol)ctx.TargetSymbol, semanticModel: ctx.SemanticModel))
            .Combine(knownTypes)
            .Where(static pair =>
            {
                var (_, knownTypes) = pair;
                return knownTypes is not null;
            })
            .Select(static (pair, cancellationToken) =>
            {
                var ((sagaType, semanticModel), knownTypes) = pair;
                return Parser.Parse(sagaType!, semanticModel!, knownTypes!, cancellationToken);
            })
            .Where(static spec => spec is not null)
            .Select(static (spec, _) => spec!)
            .WithTrackingName(TrackingNames.SagaSpec);

        var collected = addSagas.Collect()
            .Select((sagas, _) => new Sagas.SagaSpecs(sagas.ToImmutableEquatableArray()))
            .WithTrackingName(TrackingNames.SagaSpecs);

        var rootTypeSpec = AddHandlerAndSagasRegistrationGenerator.BuildRootTypeSpecPipeline(context);

        var combined = collected.Combine(rootTypeSpec);

        context.RegisterSourceOutput(combined,
            static (productionContext, spec) =>
            {
                var emitter = new Emitter(productionContext);
                emitter.Emit(spec.Left, spec.Right);
            });
    }
}