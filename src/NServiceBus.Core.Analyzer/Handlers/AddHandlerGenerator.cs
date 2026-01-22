#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Handlers;

[Generator(LanguageNames.CSharp)]
public sealed partial class AddHandlerGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var addHandlers = context.SyntaxProvider
            .ForAttributeWithMetadataName("NServiceBus.HandlerAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax classDeclarationSyntax && !classDeclarationSyntax.Modifiers.Any(SyntaxKind.AbstractKeyword),
                transform: Parser.Parse)
            .Where(static spec => spec is not null)
            .Select(static (spec, _) => spec!)
            .WithTrackingName("HandlerSpec");

        var collected = addHandlers.Collect()
            .Select((handlers, _) => new HandlerSpecs(handlers.ToImmutableEquatableArray()))
            .WithTrackingName("HandlerSpecs");

        var assemblyInfo = context.CompilationProvider
            .Select(static (compilation, _) =>
            {
                var assemblyName = compilation.AssemblyName ?? string.Empty;
                var assemblyId = AddHandlerAndSagasRegistrationGenerator.Emitter.SanitizeIdentifier(assemblyName);
                return (AssemblyName: assemblyName, AssemblyId: assemblyId);
            })
            .WithTrackingName("AssemblyInfo");

        var rootTypeSpec = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => AddHandlerAndSagasRegistrationGenerator.Parser.IsRootTypeCandidate(node),
                transform: static (ctx, _) => (ClassDeclarationSyntax)ctx.Node)
            .Combine(assemblyInfo)
            .Select(static (pair, _) => AddHandlerAndSagasRegistrationGenerator.Parser.TryGetRootTypeSpec(pair.Left, pair.Right.AssemblyName, pair.Right.AssemblyId))
            .Where(static spec => spec.HasValue)
            .Select(static (spec, _) => spec!.Value)
            .Collect()
            .Select(static (specs, _) => AddHandlerAndSagasRegistrationGenerator.Parser.SelectRootTypeSpec(specs))
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