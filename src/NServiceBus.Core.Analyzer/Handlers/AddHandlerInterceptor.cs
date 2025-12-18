#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using System.Text;
using Microsoft.CodeAnalysis;
using Utility;

[Generator(LanguageNames.CSharp)]
public sealed partial class AddHandlerInterceptor : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var addHandlers = context.SyntaxProvider
            .ForAttributeWithMetadataName("NServiceBus.NServiceBusRegistrationsAttribute",
                predicate: static (node, _) => true,
                transform: Parser.Parse)
            .SelectMany(static (spec, _) => spec)
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

    internal sealed record HandlerSpec(InterceptLocationSpec LocationSpec, HandlerTypeSpec HandlerType, ImmutableEquatableArray<MessageRegistrationSpec> Registrations);

    internal readonly record struct HandlerSpecs(ImmutableEquatableArray<HandlerSpec> Handlers);

    internal enum RegistrationType
    {
        MessageHandler,
        StartMessageHandler,
        TimeoutHandler,
    }

    internal readonly record struct MessageRegistrationSpec(RegistrationType RegistrationType, string MessageType, ImmutableEquatableArray<string> MessageHierarchy);

    internal readonly record struct HandlerTypeSpec(string FullyQualifiedName, string InterceptorMethodName)
    {
        public static HandlerTypeSpec From(INamedTypeSymbol handlerType)
        {
            const string NamePrefix = "AddHandler_";

            var sb = new StringBuilder(NamePrefix, 50)
                .Append(handlerType.Name)
                .Append('_');

            var fullyQualifiedName = handlerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var hash = NonCryptographicHash.GetHash(fullyQualifiedName);

            sb.Append(hash.ToString("x16"));

            return new HandlerTypeSpec { FullyQualifiedName = fullyQualifiedName, InterceptorMethodName = sb.ToString() };
        }
    }
}