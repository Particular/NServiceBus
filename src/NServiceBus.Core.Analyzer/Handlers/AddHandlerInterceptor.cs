#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using Microsoft.CodeAnalysis;
using Utility;
using static Handlers.Parser;

[Generator(LanguageNames.CSharp)]
public sealed partial class AddHandlerInterceptor : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var addHandlers = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => Parser.SyntaxLooksLikeAddHandlerMethod(node),
                transform: Parser.Parse)
            .Where(static spec => spec.HasValue)
            .Select((spec, _) => spec!.Value)
            .WithTrackingName("HandlerSpec");

        var collected = addHandlers.Collect()
            .Select((handlers, _) => new InterceptableHandlerSpecs(handlers.ToImmutableEquatableArray()))
            .WithTrackingName("HandlerSpecs");

        context.RegisterSourceOutput(collected,
            static (productionContext, spec) =>
            {
                var emitter = new Emitter(productionContext);
                emitter.Emit(spec);
            });
    }

    internal readonly record struct InterceptableHandlerSpec(InterceptLocationSpec LocationSpec, HandlerSpec HandlerSpec);

    internal readonly record struct InterceptableHandlerSpecs(ImmutableEquatableArray<InterceptableHandlerSpec> Handlers);
}