namespace NServiceBus.Core.Analyzer.HandlerRegistration;

using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

[Generator]
public class RegisterHandlerInterceptor : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var locations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: SyntaxLooksLikeRegisterHandlerMethod,
                transform: static (ctx, _) => new InvocationCandidate(ctx.Node.SyntaxTree.FilePath, ctx.Node.Span))
            .WithTrackingName("InterceptCandidates");

        var withCompilation = locations.Combine(context.CompilationProvider)
            .Select(GetInterceptsFromCompilation)
            .Where(static m => m is not null)
            .Select(static (x, _) => x!.Value)
            .WithTrackingName("WithCompilation");

        var collected = withCompilation.Collect()
            .WithTrackingName("Collected");

        context.RegisterSourceOutput(collected, GenerateInterceptorCode);
    }

    static bool SyntaxLooksLikeRegisterHandlerMethod(SyntaxNode node, CancellationToken cancellationToken) => node is InvocationExpressionSyntax
    {
        Expression: MemberAccessExpressionSyntax
        {
            Name: GenericNameSyntax
            {
                Identifier.ValueText: RegisterHandlerMethodName,
                TypeArgumentList.Arguments.Count: 1
            }
        },
        ArgumentList.Arguments.Count: 0
    };

    static InterceptDetails? GetInterceptsFromCompilation((InvocationCandidate, Compilation) tuple, CancellationToken cancellationToken)
    {
        var (candidate, compilation) = tuple;

        var syntaxTree = compilation.SyntaxTrees.FirstOrDefault(t => t.FilePath == candidate.FilePath);
        if (syntaxTree is null)
        {
            return null;
        }

        // Fairly expensive
        var root = syntaxTree.GetRoot(cancellationToken);
        if (root.FindNode(candidate.Span) is not InvocationExpressionSyntax invocation)
        {
            return null;
        }

        var semanticModel = compilation.GetSemanticModel(syntaxTree, ignoreAccessibility: true);
        if (semanticModel.GetOperation(invocation, cancellationToken) is not IInvocationOperation operation)
        {
            return null;
        }

        // Make sure the method we're looking at is ours and not some (extremely unlikely) copycat
        if (!IsRegisterHandlerMethod(operation.TargetMethod))
        {
            return null;
        }

        if (operation.TargetMethod.TypeArguments[0] is not INamedTypeSymbol handlerType)
        {
            return null;
        }

        var messageTypes = handlerType.AllInterfaces
            .Where(IsHandlerInterface)
            .Select(type => type.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
            .ToImmutableArray();

        if (semanticModel.GetInterceptableLocation(invocation, cancellationToken) is not { } location)
        {
            return null;
        }

        var methodName = CreateMethodName(handlerType);
        var handlerFullyQualifiedName = handlerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return new InterceptDetails(SafeInterceptionLocation.From(location), methodName, handlerFullyQualifiedName, messageTypes);
    }

    static string CreateMethodName(INamedTypeSymbol handlerType)
    {
        const string NamePrefix = "RegisterHandler_";
        const int HashBytesToUse = 10;

        var sb = new StringBuilder(NamePrefix, 50)
            .Append(handlerType.Name)
            .Append('_');

        using var sha = SHA256.Create();

        var clearBytes = Encoding.UTF8.GetBytes(handlerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        var hashBytes = sha.ComputeHash(clearBytes);

        for (var i = 0; i < HashBytesToUse; i++)
        {
            _ = sb.Append(hashBytes[i].ToString("x2"));
        }

        return sb.ToString();
    }

    static bool IsRegisterHandlerMethod(IMethodSymbol method) => method is
    {
        Name: RegisterHandlerMethodName,
        IsGenericMethod: true,
        TypeArguments: { Length: 1 },
        ContainingType:
        {
            Name: RegisterHandlerClassName,
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
            ContainingNamespace: { IsGlobalNamespace: true }
        }
    };

    static void GenerateInterceptorCode(SourceProductionContext context, ImmutableArray<InterceptDetails> intercepts)
    {
        if (intercepts.Length == 0)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("""
                      // <auto-generated/>
                      #nullable enable

                      namespace System.Runtime.CompilerServices
                      {
                          [global::System.Diagnostics.Conditional("DEBUG")]
                          [global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = true)]
                          sealed file class InterceptsLocationAttribute : global::System.Attribute
                          {
                              public InterceptsLocationAttribute(int version, string data)
                              {
                                  _ = version;
                                  _ = data;
                              }
                          }
                      }

                      namespace NServiceBus
                      {
                          static file class InterceptionsOfRegisterHandlerMethod
                          {
                      """);

        var groups = intercepts.GroupBy(i => i.MethodName);
        foreach (var group in groups)
        {
            foreach (var location in group)
            {
                sb.AppendLine($"""        [global::System.Runtime.CompilerServices.InterceptsLocation({location.Location.Version}, "{location.Location.Data}")] // {location.Location.DisplayLocation}""");
            }

            var first = group.First();
            sb.AppendLine($$"""
                                    public static void {{first.MethodName}}(NServiceBus.EndpointConfiguration endpointConfiguration)
                                    {
                                        System.ArgumentNullException.ThrowIfNull(endpointConfiguration);
                                        var registry = NServiceBus.Configuration.AdvancedExtensibility.AdvancedExtensibilityExtensions.GetSettings(endpointConfiguration)
                                            .GetOrCreate<NServiceBus.Unicast.MessageHandlerRegistry>();
                            """);
            foreach (var messageType in first.MessageTypes.Items)
            {
                sb.AppendLine($"            registry.RegisterHandlerForMessage<{first.HandlerType}, {messageType}>();");
            }
            sb.AppendLine("        }");
        }

        sb.AppendLine("""
                          }
                      }
                      """);

        context.AddSource("InterceptionsOfRegisterHandlerMethod.g.cs", sb.ToString());
    }

    const string RegisterHandlerClassName = "MessageHandlerRegistrationExtensions";
    const string RegisterHandlerMethodName = "RegisterHandler";

    record struct InvocationCandidate(string FilePath, TextSpan Span);
    record struct InterceptDetails(SafeInterceptionLocation Location, string MethodName, string HandlerType, EquatableArray<string> MessageTypes);
    record struct SafeInterceptionLocation(int Version, string Data, string DisplayLocation)
    {
        public static SafeInterceptionLocation From(InterceptableLocation location) =>
            new(location.Version, location.Data, location.GetDisplayLocation());
    }
}