#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Utility;

[Generator(LanguageNames.CSharp)]
public sealed partial class AddMultipleHandlersInterceptor : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var addHandlers = context.SyntaxProvider
            .ForAttributeWithMetadataName("NServiceBus.NServiceBusRegistrationsAttribute",
                predicate: static (node, _) => true,
                transform: Parse)
            .SelectMany(static (spec, _) => spec)
            .WithTrackingName("HandlerSpec");

        var collected = addHandlers.Collect()
            .Select((interceptions, _) => new GenerationSpec(interceptions.ToImmutableEquatableArray()))
            .WithTrackingName("HandlerSpecs");

        context.RegisterSourceOutput(collected,
            static (productionContext, spec) =>
            {
                var emitter = new Emitter(productionContext);
                emitter.Emit(spec);
            });
    }

    static ImmutableArray<AddMultipleSpec> Parse(GeneratorAttributeSyntaxContext ctx, CancellationToken cancellationToken)
    {
        var builder = ImmutableArray.CreateBuilder<AddMultipleSpec>();

        foreach (var invocation in ctx.TargetSymbol.GetDescendantsAcrossDeclarations<InvocationExpressionSyntax>(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var registrations = GetRegistrations(ctx.SemanticModel, invocation, cancellationToken);

            if (registrations is not null && ctx.SemanticModel.GetInterceptableLocation(invocation, cancellationToken) is { } location)
            {
                var locationSpec = InterceptLocationSpec.From(location);
                builder.Add(new AddMultipleSpec(locationSpec, registrations.ToImmutableEquatableArray()));
            }
        }

        return builder.ToImmutable();
    }

    static IEnumerable<RegistrationSpec>? GetRegistrations(SemanticModel semanticModel, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
    {
        if (!SyntaxLooksLikeAddMultiple(invocation))
        {
            return null;
        }

        var invocationSemanticModel = semanticModel.Compilation.GetSemanticModel(invocation.SyntaxTree);
        if (invocationSemanticModel.GetOperation(invocation, cancellationToken) is not IInvocationOperation operation)
        {
            return null;
        }

        if (!IsAddMultipleMethod(operation.TargetMethod))
        {
            return null;
        }

        var methodName = operation.TargetMethod.Name;
        var argument = ((LiteralExpressionSyntax)invocation.ArgumentList.Arguments[0].Expression).Token.ValueText;

        var compilation = semanticModel.Compilation;

        if (methodName == "AddHandlersFromAssembly")
        {
            if (compilation.AssemblyName == argument)
            {
                return EnumerateNamespaceHandlers(compilation.Assembly.GlobalNamespace);
            }

            foreach (var reference in compilation.References)
            {
                if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assemblySymbol)
                {
                    if (string.Equals(assemblySymbol.Identity.Name, argument))
                    {
                        return EnumerateNamespaceHandlers(assemblySymbol.GlobalNamespace);
                    }
                }
            }
        }
        else if (methodName == "AddHandlersFromNamespace")
        {
            var currentNamespace = compilation.GlobalNamespace;

            // Come back to this, I'd like to use span.Split() but that's in .NET 9 or System.Memory, not in netstandard2.0
            foreach (var segment in argument.Split('.'))
            {
                currentNamespace = currentNamespace.GetNamespaceMembers().FirstOrDefault(ns => ns.Name == segment);
                if (currentNamespace is null)
                {
                    return null;
                }
            }

            return EnumerateNamespaceHandlers(currentNamespace);
        }

        return null;
    }

    static IEnumerable<RegistrationSpec> EnumerateNamespaceHandlers(INamespaceSymbol ns)
    {
        foreach (var member in ns.GetMembers())
        {
            if (member is INamespaceSymbol childNamespace)
            {
                foreach (var handler in EnumerateNamespaceHandlers(childNamespace))
                {
                    yield return handler;
                }
            }
            else if (member is INamedTypeSymbol namedType)
            {
                if (namedType.AllInterfaces.Any(AddHandlerInterceptor.Parser.IsHandlerInterface))
                {
                    yield return new RegistrationSpec(namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                }
            }
            else
            {
                throw new Exception(member.GetType().ToString());
            }
        }
    }

    static bool SyntaxLooksLikeAddMultiple(SyntaxNode node) => node is InvocationExpressionSyntax
    {
        Expression: MemberAccessExpressionSyntax
        {
            Name: IdentifierNameSyntax
            {
                Identifier.ValueText: "AddHandlersFromAssembly" or "AddHandlersFromNamespace"
            }
        },
        ArgumentList.Arguments: [{ Expression: LiteralExpressionSyntax }]
    };

    static bool IsAddMultipleMethod(IMethodSymbol method) => method is
    {
        Name: "AddHandlersFromAssembly" or "AddHandlersFromNamespace",
        IsGenericMethod: false,
        TypeArguments.Length: 0,
        ContainingType:
        {
            Name: "MessageHandlerRegistrationExtensions",
            ContainingNamespace:
            {
                Name: "NServiceBus",
                ContainingNamespace.IsGlobalNamespace: true
            }
        }
    };

    internal record GenerationSpec(ImmutableEquatableArray<AddMultipleSpec> Methods);
    internal record AddMultipleSpec(InterceptLocationSpec LocationSpec, ImmutableEquatableArray<RegistrationSpec> Registrations);
    internal record RegistrationSpec(string TypeName);

    internal class Emitter(SourceProductionContext sourceProductionContext)
    {
        public void Emit(GenerationSpec specs)
        {
            if (specs.Methods.Count == 0)
            {
                return;
            }

            var sourceWriter = new SourceWriter()
                .ForInterceptor()
                .WithGeneratedCodeAttribute();

            sourceWriter.WriteLine("""
                                   static file class InterceptionsOfAddMultipleHandlers
                                   {
                                   """);
            sourceWriter.Indentation++;

            sourceWriter.WriteLine("""
                                   extension (NServiceBus.EndpointConfiguration endpointConfiguration)
                                   {
                                   """);
            sourceWriter.Indentation++;

            int i = 0;
            foreach (var method in specs.Methods)
            {
                sourceWriter.WriteLine($"{method.LocationSpec.Attribute} // {method.LocationSpec.DisplayLocation}");
                sourceWriter.WriteLine($"public void RegisterMultipleHandlers_{++i}()");
                sourceWriter.WriteLine("{");
                sourceWriter.Indentation++;
                foreach (var registration in method.Registrations)
                {
                    sourceWriter.WriteLine($"// Register {registration.TypeName}");
                }
            }

            sourceWriter.CloseCurlies();

            sourceProductionContext.AddSource("InterceptionsOfAddMultipleHandlers.g.cs", sourceWriter.ToSourceText());
        }
    }

}