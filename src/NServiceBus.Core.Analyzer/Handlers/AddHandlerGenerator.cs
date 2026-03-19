#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NServiceBus.Core.Analyzer;
using static Handlers;

[Generator(LanguageNames.CSharp)]
public sealed partial class AddHandlerGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var knownTypes = KnownTypePipelines.BuildHandlerKnownTypesPipeline(context);

        var addHandlers = context.SyntaxProvider
            .ForAttributeWithMetadataName("NServiceBus.HandlerAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax classDeclarationSyntax && !classDeclarationSyntax.Modifiers.Any(SyntaxKind.AbstractKeyword) && !classDeclarationSyntax.Modifiers.Any(SyntaxKind.StaticKeyword),
                transform: static (ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol)
            .Combine(knownTypes)
            .Where(static pair =>
            {
                var (_, knownTypes) = pair;
                return knownTypes is not null;
            })
            .Select(static (pair, cancellationToken) =>
            {
                var (handlerType, knownTypes) = pair;
                return Parser.Parse(handlerType, knownTypes!, cancellationToken);
            })
            .WithTrackingName(TrackingNames.HandlerSpec);

        var collected = addHandlers.Collect()
            .Select((handlers, _) => new HandlerSpecs(handlers.ToImmutableEquatableArray()))
            .WithTrackingName(TrackingNames.HandlerSpecs);

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