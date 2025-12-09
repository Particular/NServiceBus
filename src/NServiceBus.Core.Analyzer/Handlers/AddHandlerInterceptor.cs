#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using Microsoft.CodeAnalysis;
using Utility;
using static NServiceBus.Core.Analyzer.ManualRegistrations;

[Generator(LanguageNames.CSharp)]
public sealed partial class AddHandlerInterceptor : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var registrationTargets = CreateTargets(context);

        var addHandlers = registrationTargets
            .Combine(context.CompilationProvider)
            .SelectMany(static (targetAndCompilation, cancellationToken) =>
                GetSpecs(targetAndCompilation.Left, targetAndCompilation.Right,
                    Parser.SyntaxLooksLikeAddHandlerMethod,
                    static (semanticModel, invocation, ct) => Parser.Parse(semanticModel, invocation, ct),
                    cancellationToken))
            .WithTrackingName("HandlerSpec");

        var collected = addHandlers.Collect()
            .Select((handlers, _) => new HandlerSpecs(handlers.ToImmutableEquatableArray()))
            .WithTrackingName("HandlerSpecs");

        context.RegisterSourceOutput(collected,
            static (productionContext, spec) =>
            {
                var emitter = new Emitter(productionContext);
                emitter.Emit(spec);
            });
    }

    internal sealed record HandlerSpec(InterceptLocationSpec LocationSpec, string Name, string HandlerType, ImmutableEquatableArray<RegistrationSpec> Registrations);

    internal readonly record struct HandlerSpecs(ImmutableEquatableArray<HandlerSpec> Handlers);

    internal enum RegistrationType
    {
        MessageHandler,
        StartMessageHandler,
        TimeoutHandler,
    }

    internal readonly record struct RegistrationSpec(RegistrationType RegistrationType, string MessageType);
}
