#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using Microsoft.CodeAnalysis;

[Generator(LanguageNames.CSharp)]
public sealed partial class AddHandlerGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var addHandlers = context.SyntaxProvider
            .ForAttributeWithMetadataName("NServiceBus.HandlerAttribute",
                predicate: static (node, _) => true,
                transform: Parser.Parse)
            .Where(static spec => spec is not null)
            .Select(static (spec, _) => spec!)
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

    internal sealed record HandlerSpec(
        string Name,
        string HandlerType,
        string HandlerNamespace,
        string AssemblyName,
        ImmutableEquatableArray<RegistrationSpec> Registrations);

    internal readonly record struct HandlerSpecs(ImmutableEquatableArray<HandlerSpec> Handlers);

    internal enum RegistrationType
    {
        MessageHandler,
        StartMessageHandler,
        TimeoutHandler,
    }

    internal readonly record struct RegistrationSpec(RegistrationType RegistrationType, string MessageType, ImmutableEquatableArray<string> MessageHierarchy, string HandlerType);
}