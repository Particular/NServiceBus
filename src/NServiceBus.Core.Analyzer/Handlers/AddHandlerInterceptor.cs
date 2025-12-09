#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Utility;

[Generator(LanguageNames.CSharp)]
public sealed partial class AddHandlerInterceptor : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var methodLevel = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "NServiceBus.NServiceBusRegistrationsAttribute",
                predicate: static (node, _) =>
                    node is MethodDeclarationSyntax
                    {
                        ParameterList.Parameters: var parameters
                    } methodSyntax
                    && parameters.Any(p =>
                        p.Type is IdentifierNameSyntax { Identifier.ValueText: "EndpointConfiguration" } or
                        QualifiedNameSyntax
                        {
                            Right.Identifier.ValueText: "EndpointConfiguration"
                        }) && Parser.SyntaxLooksLikeAddHandlerMethod(methodSyntax),
                transform: static (ctx, ct) => Parser.Parse(ctx.SemanticModel, (MethodDeclarationSyntax)ctx.TargetNode, ct))
            .SelectMany(static (spec, _) => spec)
            .Collect();

        var typeLevel = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "NServiceBus.NServiceBusRegistrationsAttribute",
                predicate: static (node, _) => node is TypeDeclarationSyntax,
                transform: static (ctx, ct) =>
                {
                    if (ctx.TargetNode is not TypeDeclarationSyntax typeSyntax)
                    {
                        return [];
                    }

                    var semanticModel = ctx.SemanticModel;
                    var specs = ImmutableArray.CreateBuilder<HandlerSpec>();
                    foreach (var member in typeSyntax.Members.OfType<MethodDeclarationSyntax>())
                    {
                        if (!Parser.SyntaxLooksLikeAddHandlerMethod(member))
                        {
                            continue;
                        }

                        specs.AddRange(Parser.Parse(semanticModel, member, ct));
                    }
                    return specs.ToImmutable();
                })
            .SelectMany(static (spec, _) => spec)
            .Collect();

        var assemblyLevel = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "NServiceBus.NServiceBusRegistrationsAttribute",
                predicate: static (node, _) => node is CompilationUnitSyntax,
                transform: static (ctx, ct) =>
                {
                    var compilation = ctx.SemanticModel.Compilation;

                    var specs = ImmutableArray.CreateBuilder<HandlerSpec>();

                    foreach (var tree in compilation.SyntaxTrees)
                    {
                        ct.ThrowIfCancellationRequested();

                        var semanticModel = compilation.GetSemanticModel(tree);
                        var root = tree.GetRoot(ct);

                        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
                        {
                            if (!Parser.SyntaxLooksLikeAddHandlerMethod(method))
                            {
                                continue;
                            }

                            specs.AddRange(Parser.Parse(semanticModel, method, ct));
                        }
                    }

                    return specs.ToImmutable();
                })
            .SelectMany(static (spec, _) => spec)
            .Collect();

        var collected = methodLevel
            .Combine(typeLevel)
            .Combine(assemblyLevel)
            .Select((triplet, _) =>
            {
                var ((methods, types), assembly) = triplet;
                return new HandlerSpecs(methods.Union(types).Union(assembly).ToImmutableEquatableArray());
            })
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