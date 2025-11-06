#nullable enable
namespace NServiceBus.Core.Analyzer.Sagas;

using System.Collections.Generic;
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
using NServiceBus.Core.Analyzer;

[Generator]
public class AddSagaInterceptor : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var locations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: SyntaxLooksLikeAddSagaMethod,
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

    static bool SyntaxLooksLikeAddSagaMethod(SyntaxNode node, CancellationToken cancellationToken) => node is InvocationExpressionSyntax
    {
        Expression: MemberAccessExpressionSyntax
        {
            Name: GenericNameSyntax
            {
                Identifier.ValueText: AddSagaMethodName,
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
        if (!IsAddSagaMethod(operation.TargetMethod))
        {
            return null;
        }

        if (operation.TargetMethod.TypeArguments[0] is not INamedTypeSymbol sagaType)
        {
            return null;
        }

        // Verify this is actually a saga type
        if (!IsSagaType(sagaType))
        {
            return null;
        }

        // Get saga data type
        var sagaDataType = GetSagaDataType(sagaType);
        if (sagaDataType is null)
        {
            return null;
        }

        // Extract handler interfaces (for handler registration)
        var handlerRegistrations = sagaType.AllInterfaces
            .Where(IsHandlerInterface)
            .Select(type =>
            {
                var messageType = type.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var addType = type.Name == "IHandleTimeouts" ? "Timeout" : "Message";
                return new MessageRegistration(addType, messageType);
            })
            .ToImmutableArray();

        // Extract correlation mappings from ConfigureHowToFindSaga
        var correlations = ExtractCorrelations(sagaType, sagaDataType, semanticModel, cancellationToken);

        if (semanticModel.GetInterceptableLocation(invocation, cancellationToken) is not { } location)
        {
            return null;
        }

        var methodName = CreateMethodName(sagaType);
        var sagaFullyQualifiedName = sagaType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var sagaDataFullyQualifiedName = sagaDataType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return new InterceptDetails(
            SafeInterceptionLocation.From(location),
            methodName,
            sagaFullyQualifiedName,
            sagaDataFullyQualifiedName,
            handlerRegistrations,
            correlations);
    }

    static bool IsSagaType(INamedTypeSymbol type)
    {
        // Check if type inherits from Saga
        var baseType = type.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == "Saga" && baseType.ContainingNamespace.Name == "NServiceBus")
            {
                return true;
            }
            baseType = baseType.BaseType;
        }
        return false;
    }

    static INamedTypeSymbol? GetSagaDataType(INamedTypeSymbol sagaType)
    {
        // Find the base Saga<TData> type
        var baseType = sagaType.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == "Saga" && baseType.IsGenericType && baseType.TypeArguments.Length == 1)
            {
                if (baseType.TypeArguments[0] is INamedTypeSymbol sagaData)
                {
                    return sagaData;
                }
            }
            baseType = baseType.BaseType;
        }
        return null;
    }

    static EquatableArray<CorrelationMapping> ExtractCorrelations(
        INamedTypeSymbol sagaType,
        INamedTypeSymbol sagaDataType,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        // Find ConfigureHowToFindSaga method
        var configureMethod = sagaType.GetMembers("ConfigureHowToFindSaga")
            .OfType<IMethodSymbol>()
            .FirstOrDefault();

        if (configureMethod == null)
        {
            return new EquatableArray<CorrelationMapping>(ImmutableArray<CorrelationMapping>.Empty);
        }

        // Get method syntax
        var syntaxReference = configureMethod.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxReference == null)
        {
            return new EquatableArray<CorrelationMapping>(ImmutableArray<CorrelationMapping>.Empty);
        }

        if (syntaxReference.GetSyntax(cancellationToken) is not MethodDeclarationSyntax methodSyntax)
        {
            return new EquatableArray<CorrelationMapping>(ImmutableArray<CorrelationMapping>.Empty);
        }

        var methodSemanticModel = semanticModel;
        var methodSyntaxTree = syntaxReference.SyntaxTree;
        if (methodSyntaxTree != semanticModel.SyntaxTree)
        {
            // Method is in a different file, need to get semantic model for that tree
            var compilation = semanticModel.Compilation;
            methodSemanticModel = compilation.GetSemanticModel(methodSyntaxTree, ignoreAccessibility: true);
        }

        var correlations = new List<CorrelationMapping>();

        // Find all ConfigureMapping calls
        var invocations = methodSyntax.DescendantNodes()
            .OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocations)
        {
            var correlation = TryExtractCorrelation(invocation, sagaType, sagaDataType, methodSemanticModel, cancellationToken);
            if (correlation != null)
            {
                correlations.Add(correlation.Value);
            }
        }

        return new EquatableArray<CorrelationMapping>(correlations.ToImmutableArray());
    }

    static CorrelationMapping? TryExtractCorrelation(
        InvocationExpressionSyntax invocation,
        INamedTypeSymbol sagaType,
        INamedTypeSymbol _,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        // Check if this is a ConfigureMapping call
        if (semanticModel.GetOperation(invocation, cancellationToken) is not IInvocationOperation operation)
        {
            return null;
        }

        var method = operation.TargetMethod;
        if (method.Name != "ConfigureMapping" || !method.IsGenericMethod)
        {
            return null;
        }

        // Get the message type from ConfigureMapping<TMessage>
        if (method.TypeArguments.Length != 1)
        {
            return null;
        }

        var messageType = method.TypeArguments[0];
        if (messageType is not INamedTypeSymbol messageTypeSymbol)
        {
            return null;
        }

        // Check if this is part of a chain like: mapper.ConfigureMapping<T>(m => m.Prop).ToSaga(s => s.Prop)
        // We need to find the ToSaga call
        var parent = invocation.Parent;
        while (parent != null)
        {
            if (parent is InvocationExpressionSyntax parentInvocation)
            {
                if (semanticModel.GetOperation(parentInvocation, cancellationToken) is IInvocationOperation parentOperation)
                {
                    if (parentOperation.TargetMethod.Name == "ToSaga")
                    {
                        // Found the chain, extract properties
                        var messageProperty = ExtractPropertyFromLambda(invocation.ArgumentList.Arguments[0]);
                        var sagaProperty = ExtractPropertyFromLambda(parentInvocation.ArgumentList.Arguments[0]);

                        if (messageProperty != null && sagaProperty != null)
                        {
                            // Check if message can start saga
                            var canStartSaga = CanMessageStartSaga(sagaType, messageTypeSymbol);

                            return new CorrelationMapping
                            {
                                MessageType = messageTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                MessagePropertyName = messageProperty,
                                SagaPropertyName = sagaProperty,
                                PropertyType = ExtractPropertyType(messageTypeSymbol, messageProperty),
                                CanStartSaga = canStartSaga
                            };
                        }
                    }
                }
            }
            parent = parent.Parent;
        }

        return null;
    }

    static string? ExtractPropertyFromLambda(ArgumentSyntax argument)
    {
        if (argument.Expression is not LambdaExpressionSyntax lambda)
        {
            return null;
        }

        // Handle: m => m.PropertyName or s => s.PropertyName
        if (lambda.Body is MemberAccessExpressionSyntax memberAccess)
        {
            return memberAccess.Name.Identifier.ValueText;
        }

        // Handle: m => ((SomeType)m).PropertyName (cast expressions)
        if (lambda.Body is ParenthesizedExpressionSyntax parenExpr &&
            parenExpr.Expression is MemberAccessExpressionSyntax castMemberAccess)
        {
            return castMemberAccess.Name.Identifier.ValueText;
        }

        return null;
    }

    static string ExtractPropertyType(INamedTypeSymbol messageType, string propertyName)
    {
        var property = messageType.GetMembers(propertyName).OfType<IPropertySymbol>().FirstOrDefault();
        if (property != null)
        {
            return property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }
        return "object";
    }

    static bool CanMessageStartSaga(INamedTypeSymbol sagaType, INamedTypeSymbol messageType)
    {
        // Check if saga implements IAmStartedByMessages<TMessage>
        var iAmStartedByMessages = sagaType.AllInterfaces
            .FirstOrDefault(i => i.Name == "IAmStartedByMessages" &&
                                 i.IsGenericType &&
                                 i.TypeArguments.Length == 1 &&
                                 i.TypeArguments[0].Equals(messageType, SymbolEqualityComparer.Default));

        return iAmStartedByMessages != null;
    }

    static string CreateMethodName(INamedTypeSymbol sagaType)
    {
        const string NamePrefix = "AddSaga_";
        const int HashBytesToUse = 10;

        var sb = new StringBuilder(NamePrefix, 50)
            .Append(sagaType.Name)
            .Append('_');

        using var sha = SHA256.Create();

        var clearBytes = Encoding.UTF8.GetBytes(sagaType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        var hashBytes = sha.ComputeHash(clearBytes);

        for (var i = 0; i < HashBytesToUse; i++)
        {
            _ = sb.Append(hashBytes[i].ToString("x2"));
        }

        return sb.ToString();
    }

    static bool IsAddSagaMethod(IMethodSymbol method) => method is
    {
        Name: AddSagaMethodName,
        IsGenericMethod: true,
        TypeArguments: { Length: 1 },
        ContainingType:
        {
            Name: AddSagaClassName,
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
                          static file class InterceptionsOfAddSagaMethod
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
                                        
                                        // Register handlers via MessageHandlerRegistry (David's infrastructure)
                                        var registry = NServiceBus.Configuration.AdvancedExtensibility.AdvancedExtensibilityExtensions.GetSettings(endpointConfiguration)
                                            .GetOrCreate<NServiceBus.Unicast.MessageHandlerRegistry>();
                            """);
            foreach (var registration in first.HandlerRegistrations.Items)
            {
                sb.AppendLine($"            registry.Add{registration.AddType}HandlerForMessage<{first.SagaType}, {registration.MessageType}>();");
            }

            // Register saga metadata
            sb.AppendLine();
            sb.AppendLine("            // Register saga metadata");
            sb.AppendLine($"            var metadata = NServiceBus.SagaRegistrationExtensions.RegisterSagaMetadata<{first.SagaType}, {first.SagaDataType}>(endpointConfiguration);");

            foreach (var correlation in first.Correlations.Items)
            {
                sb.AppendLine($"            metadata.WithCorrelation<{correlation.MessageType}, {correlation.PropertyType}>(");
                sb.AppendLine($"                message => message.{correlation.MessagePropertyName},");
                sb.AppendLine($"                saga => saga.{correlation.SagaPropertyName},");
                sb.AppendLine($"                canStartSaga: {(correlation.CanStartSaga ? "true" : "false")});");
            }

            sb.AppendLine("            metadata.Complete();");
            sb.AppendLine("        }");
        }

        sb.AppendLine("""
                          }
                      }
                      """);

        context.AddSource("InterceptionsOfAddSagaMethod.g.cs", sb.ToString());
    }

    const string AddSagaClassName = "SagaRegistrationExtensions";
    const string AddSagaMethodName = "AddSaga";

    record struct InvocationCandidate(string FilePath, TextSpan Span);
    record struct InterceptDetails(
        SafeInterceptionLocation Location,
        string MethodName,
        string SagaType,
        string SagaDataType,
        EquatableArray<MessageRegistration> HandlerRegistrations,
        EquatableArray<CorrelationMapping> Correlations);
    record struct MessageRegistration(string AddType, string MessageType);
    record struct CorrelationMapping
    {
        public string MessageType { get; init; }
        public string MessagePropertyName { get; init; }
        public string SagaPropertyName { get; init; }
        public string PropertyType { get; init; }
        public bool CanStartSaga { get; init; }
    }
    record struct SafeInterceptionLocation(int Version, string Data, string DisplayLocation)
    {
        public static SafeInterceptionLocation From(InterceptableLocation location) =>
            new(location.Version, location.Data, location.GetDisplayLocation());
    }
}

