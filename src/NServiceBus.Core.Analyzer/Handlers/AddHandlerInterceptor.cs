#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Utility;

[Generator]
public class AddHandlerInterceptor : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var addHandlers = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: SyntaxLooksLikeAddHandlerMethod,
                transform: TransformInvocation)
            .Where(static d => d is not null)
            .Select(static (d, _) => d!)
            .WithTrackingName("InterceptCandidates");

        var collected = addHandlers.Collect()
            .WithTrackingName("Collected");

        context.RegisterSourceOutput(collected, GenerateInterceptorCode);
    }

    static bool SyntaxLooksLikeAddHandlerMethod(SyntaxNode node, CancellationToken cancellationToken) => node is InvocationExpressionSyntax
    {
        Expression: MemberAccessExpressionSyntax
        {
            Name: GenericNameSyntax
            {
                Identifier.ValueText: AddHandlerMethodName,
                TypeArgumentList.Arguments.Count: 1
            }
        },
        ArgumentList.Arguments.Count: 0
    };

    static InterceptDetails? TransformInvocation(GeneratorSyntaxContext ctx, CancellationToken cancellationToken)
    {
        var invocation = (InvocationExpressionSyntax)ctx.Node;

        if (ctx.SemanticModel.GetOperation(invocation, cancellationToken) is not IInvocationOperation operation)
        {
            return null;
        }

        // Make sure the method we're looking at is ours and not some (extremely unlikely) copycat
        if (!IsAddHandlerMethod(operation.TargetMethod))
        {
            return null;
        }

        if (operation.TargetMethod.TypeArguments[0] is not INamedTypeSymbol handlerType)
        {
            return null;
        }

        var registrations = handlerType.AllInterfaces
            .Where(IsHandlerInterface)
            .Select(type =>
            {
                var messageType = type.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var addType = type.Name == "IHandleTimeouts" ? "Timeout" : "Message";
                return new MessageRegistration(addType, messageType);
            })
            .ToImmutableArray();

        if (ctx.SemanticModel.GetInterceptableLocation(invocation, cancellationToken) is not { } location)
        {
            return null;
        }

        var methodName = CreateMethodName(handlerType);
        var handlerFullyQualifiedName = handlerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return new InterceptDetails(SafeInterceptionLocation.From(location), methodName, handlerFullyQualifiedName, registrations);
    }

    static string CreateMethodName(INamedTypeSymbol handlerType)
    {
        const string NamePrefix = "AddHandler_";

        var sb = new StringBuilder(NamePrefix, 50)
            .Append(handlerType.Name)
            .Append('_');

        var handlerFullName = handlerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var hash = NonCryptographicHash.GetHash(handlerFullName);

        sb.Append(hash.ToString("x16"));

        return sb.ToString();
    }

    static bool IsAddHandlerMethod(IMethodSymbol method) => method is
    {
        Name: AddHandlerMethodName,
        IsGenericMethod: true,
        TypeArguments.Length: 1,
        ContainingType:
        {
            Name: AddHandlerClassName,
            ContainingNamespace:
            {
                Name: "NServiceBus",
                ContainingNamespace.IsGlobalNamespace: true
            }
        }
    };

    static bool IsHandlerInterface(INamedTypeSymbol type) => type is
    {
        Name: "IHandleMessages" or "IHandleTimeouts",
        IsGenericType: true,
        ContainingNamespace:
        {
            Name: "NServiceBus",
            ContainingNamespace.IsGlobalNamespace: true
        }
    };

    static void GenerateInterceptorCode(SourceProductionContext context, ImmutableArray<InterceptDetails> intercepts)
    {
        if (intercepts.Length == 0)
        {
            return;
        }

        var sourceWriter = new SourceWriter().ForInterceptor();

        sourceWriter.WriteLine($$"""[global::System.CodeDom.Compiler.GeneratedCodeAttribute("{{assemblyName.Name}}", "{{assemblyName.Version}}")]""");
        sourceWriter.WriteLine("""
                               static file class InterceptionsOfAddHandlerMethod
                               {
                               """);

        sourceWriter.Indentation++;

        var groups = intercepts.GroupBy(i => i.MethodName).OrderBy(g => g.Key, StringComparer.Ordinal);
        foreach (IGrouping<string, InterceptDetails> group in groups)
        {
            foreach (InterceptDetails location in group)
            {
                sourceWriter.WriteLine($"{location.Location.Attribute} // {location.Location.DisplayLocation}");
            }

            InterceptDetails first = group.First();
            sourceWriter.WriteLine($$"""
                                    public static void {{first.MethodName}}(this NServiceBus.EndpointConfiguration endpointConfiguration)
                                    {
                                    """);
            sourceWriter.Indentation++;
            sourceWriter.WriteLine("""
                                    System.ArgumentNullException.ThrowIfNull(endpointConfiguration);
                                    var registry = NServiceBus.Configuration.AdvancedExtensibility.AdvancedExtensibilityExtensions.GetSettings(endpointConfiguration)
                                       .GetOrCreate<NServiceBus.Unicast.MessageHandlerRegistry>();
                                    """);
            foreach (var registration in first.Registrations.Items)
            {
                sourceWriter.WriteLine($"registry.Add{registration.AddType}HandlerForMessage<{first.HandlerType}, {registration.MessageType}>();");
            }

            sourceWriter.Indentation--;
            sourceWriter.WriteLine("}");
        }

        sourceWriter.CloseCurlies();

        context.AddSource("InterceptionsOfAddHandlerMethod.g.cs", sourceWriter.ToSourceText());
    }

    const string AddHandlerClassName = "MessageHandlerRegistrationExtensions";
    const string AddHandlerMethodName = "AddHandler";
    static readonly AssemblyName assemblyName = typeof(AddHandlerInterceptor).Assembly.GetName();

    record InterceptDetails(SafeInterceptionLocation Location, string MethodName, string HandlerType, EquatableArray<MessageRegistration> Registrations);
    readonly record struct MessageRegistration(string AddType, string MessageType);
    readonly record struct SafeInterceptionLocation(string Attribute, string DisplayLocation)
    {
        public static SafeInterceptionLocation From(InterceptableLocation location) =>
            new(location.GetInterceptsLocationAttributeSyntax(), location.GetDisplayLocation());
    }
}