#nullable enable

namespace NServiceBus.Core.Analyzer;

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
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
            .Select(static (spec, _) => spec!.Value)
            .WithTrackingName("HandlerSpec");

        var addSagas = context.SyntaxProvider
            .ForAttributeWithMetadataName("NServiceBus.SagaAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax classDeclarationSyntax && !classDeclarationSyntax.Modifiers.Any(SyntaxKind.AbstractKeyword),
                transform: Parser.Parse)
            .Where(static spec => spec is not null)
            .Select(static (spec, _) => spec!.Value)
            .WithTrackingName("SagaSpec");

        var collected = addHandlers.Collect()
            .Combine(addSagas.Collect())
            .Select((pair, _) => pair.Left.AddRange(pair.Right))
            .WithTrackingName("HandlerAndSagaSpecs");

        context.RegisterSourceOutput(collected,
            static (productionContext, spec) =>
            {
                var emitter = new Emitter(productionContext);
                emitter.Emit(spec);
            });
    }
}