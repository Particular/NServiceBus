#nullable enable

namespace NServiceBus.Core.Analyzer.Sagas;

using Microsoft.CodeAnalysis;

[Generator(LanguageNames.CSharp)]
public sealed partial class AddSagaInterceptor : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var addSagas = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => Parser.SyntaxLooksLikeAddSagaMethod(node),
                transform: Parser.Parse)
            .Where(static d => d is not null)
            .Select(static (d, _) => d!)
            .WithTrackingName("InterceptCandidates");

        var collected = addSagas.Collect()
            .WithTrackingName("Collected");

        context.RegisterSourceOutput(collected,
            static (productionContext, intercepts) =>
            {
                var emitter = new Emitter(productionContext);
                emitter.Emit(intercepts);
            });
    }
}