#nullable enable

namespace NServiceBus.Core.Analyzer.Handlers;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

        var iMessage = ctx.SemanticModel.Compilation.GetTypeByMetadataName("NServiceBus.IMessage");
        var iCommand = ctx.SemanticModel.Compilation.GetTypeByMetadataName("NServiceBus.ICommand");
        var iEvent = ctx.SemanticModel.Compilation.GetTypeByMetadataName("NServiceBus.IEvent");

        var registrations = handlerType.AllInterfaces
            .Where(IsHandlerInterface)
            .Select(type =>
            {
                var builder = ImmutableArray.CreateBuilder<MessageMetadata>();

                ITypeSymbol messageType = type.TypeArguments[0];

                var candidateTypes = Enumerable
                    .Repeat(messageType, 1)
                    .Concat(GetParentTypes((INamedTypeSymbol)messageType))
                    .OfType<INamedTypeSymbol>()
                    .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)
                    .OrderByDescending(PlaceInMessageHierarchy)
                    .ThenBy(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal);

                foreach (var candidate in candidateTypes)
                {
                    if (SymbolEqualityComparer.Default.Equals(candidate, iMessage) ||
                        SymbolEqualityComparer.Default.Equals(candidate, iCommand) ||
                        SymbolEqualityComparer.Default.Equals(candidate, iEvent))
                    {
                        continue;
                    }
                    builder.Add(new MessageMetadata(candidate.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), IsProbablyMessageType(candidate, iMessage, iCommand, iEvent)));
                }

                var messageTypeName = messageType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var addType = type.Name == "IHandleTimeouts" ? "Timeout" : "Message";
                return new MessageRegistration(addType, new MessageMetadata(messageTypeName, IsProbablyMessageType(type, iMessage, iCommand, iEvent)), builder.ToImmutable());
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

    // We approximate conventions by checking the core marker interfaces.
    static bool IsProbablyMessageType(INamedTypeSymbol type, INamedTypeSymbol? iMessage, INamedTypeSymbol? iCommand, INamedTypeSymbol? iEvent)
    {
        return (iMessage is not null && Implements(type, iMessage)) ||
               (iCommand is not null && Implements(type, iCommand)) ||
               (iEvent is not null && Implements(type, iEvent));

        static bool Implements(INamedTypeSymbol t, INamedTypeSymbol marker) =>
            SymbolEqualityComparer.Default.Equals(t, marker) ||
            t.AllInterfaces.Contains(marker, SymbolEqualityComparer.Default);
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

    static IEnumerable<INamedTypeSymbol> GetParentTypes(INamedTypeSymbol type)
    {
        // All interfaces implemented by the type (includes inherited interfaces)
        foreach (var iface in type.AllInterfaces)
        {
            yield return iface;
        }

        // All base types up to but excluding System.Object
        var currentBase = type.BaseType;
        while (currentBase is { SpecialType: not SpecialType.System_Object })
        {
            if (currentBase is { } named)
            {
                yield return named;
            }

            currentBase = currentBase.BaseType;
        }
    }

    static int PlaceInMessageHierarchy(INamedTypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Interface)
        {
            // Approximate: number of interfaces implemented by this interface
            return type.AllInterfaces.Length;
        }

        var result = 0;
        var current = type.BaseType;
        while (current is not null)
        {
            result++;
            current = current.BaseType;
        }

        return result;
    }

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
                          static file class InterceptionsOfAddHandlerMethod
                          {
                      """);

        var groups = intercepts.GroupBy(i => i.MethodName)
            .OrderBy(g => g.Key, StringComparer.Ordinal);
        foreach (IGrouping<string, InterceptDetails> group in groups)
        {
            foreach (InterceptDetails location in group)
            {
                sb.AppendLine($"        {location.Location.Attribute} // {location.Location.DisplayLocation}");
            }

            InterceptDetails first = group.First();
            sb.AppendLine($$"""
                                    public static void {{first.MethodName}}(NServiceBus.EndpointConfiguration endpointConfiguration)
                                    {
                                        System.ArgumentNullException.ThrowIfNull(endpointConfiguration);
                                        var messageMetadataRegistry = NServiceBus.Configuration.AdvancedExtensibility.AdvancedExtensibilityExtensions.GetSettings(endpointConfiguration)
                                            .Get<NServiceBus.Unicast.Messages.MessageMetadataRegistry>();
                                        var registry = NServiceBus.Configuration.AdvancedExtensibility.AdvancedExtensibilityExtensions.GetSettings(endpointConfiguration)
                                            .GetOrCreate<NServiceBus.Unicast.MessageHandlerRegistry>();
                            """);
            foreach (var registration in first.Registrations.Items)
            {
                sb.AppendLine($"            registry.Add{registration.AddType}HandlerForMessage<{first.HandlerType}, {registration.MessageType.MessageType}>();");
                var hierarchyItems = registration.Hierarchy.Items
                    .Select(h => $"typeof({h.MessageType})");

                var hierarchyLiteral = $"[{string.Join(", ", hierarchyItems)}]";
                sb.AppendLine($"            messageMetadataRegistry.RegisterMetadata(typeof({registration.MessageType.MessageType}), {hierarchyLiteral});");
            }
            sb.AppendLine("        }");
        }

        sb.AppendLine("""
                          }
                      }
                      """);

        context.AddSource("InterceptionsOfAddHandlerMethod.g.cs", sb.ToString());
    }

    const string AddHandlerClassName = "MessageHandlerRegistrationExtensions";
    const string AddHandlerMethodName = "AddHandler";

    record InterceptDetails(SafeInterceptionLocation Location, string MethodName, string HandlerType, EquatableArray<MessageRegistration> Registrations);
    readonly record struct MessageRegistration(string AddType, MessageMetadata MessageType, EquatableArray<MessageMetadata> Hierarchy);
    readonly record struct MessageMetadata(string MessageType, bool ProbablyMessageType);
    readonly record struct SafeInterceptionLocation(string Attribute, string DisplayLocation)
    {
        public static SafeInterceptionLocation From(InterceptableLocation location) =>
            new(location.GetInterceptsLocationAttributeSyntax(), location.GetDisplayLocation());
    }
}