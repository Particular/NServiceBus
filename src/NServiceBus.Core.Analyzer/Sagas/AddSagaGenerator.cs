#nullable enable

namespace NServiceBus.Core.Analyzer.Sagas;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[Generator(LanguageNames.CSharp)]
public sealed partial class AddSagaGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var addSagas = context.SyntaxProvider
            .ForAttributeWithMetadataName("NServiceBus.SagaAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax classDeclarationSyntax && !classDeclarationSyntax.Modifiers.Any(SyntaxKind.AbstractKeyword),
                transform: Parser.Parse)
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