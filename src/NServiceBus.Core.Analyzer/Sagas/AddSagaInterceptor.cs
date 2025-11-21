#nullable enable

namespace NServiceBus.Core.Analyzer.Sagas;

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
public class AddSagaInterceptor : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var addSagas = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: SyntaxLooksLikeAddSagaMethod,
                transform: TransformInvocation)
            .Where(static d => d is not null)
            .Select(static (d, _) => d!)
            .WithTrackingName("InterceptCandidates");

        var collected = addSagas.Collect()
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

    static InterceptDetails? TransformInvocation(GeneratorSyntaxContext ctx, CancellationToken cancellationToken)
    {
        var invocation = (InvocationExpressionSyntax)ctx.Node;

        if (ctx.SemanticModel.GetOperation(invocation, cancellationToken) is not IInvocationOperation operation)
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

        // Extract saga data type from Saga<TSagaData>
        var sagaDataType = GetSagaDataType(sagaType);
        if (sagaDataType == null)
        {
            return null;
        }

        // Get interceptable location for code generation
        if (ctx.SemanticModel.GetInterceptableLocation(invocation, cancellationToken) is not { } location)
        {
            return null;
        }

        var methodName = CreateMethodName(sagaType);
        var sagaFullyQualifiedName = sagaType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var sagaDataFullyQualifiedName = sagaDataType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Analyze ConfigureHowToFindSaga to extract mappings
        var propertyMappings = ExtractPropertyMappings(sagaType, sagaDataType, ctx.SemanticModel, cancellationToken);
        var headerMappings = ExtractHeaderMappings(sagaType, sagaDataType, ctx.SemanticModel, cancellationToken);
        var messages = ExtractMessageTypes(sagaType, cancellationToken);

        return new InterceptDetails(
            SafeInterceptionLocation.From(location),
            methodName,
            sagaFullyQualifiedName,
            sagaDataFullyQualifiedName,
            propertyMappings,
            headerMappings,
            messages);
    }

    static INamedTypeSymbol? GetSagaDataType(INamedTypeSymbol sagaType)
    {
        // Find Saga<TSagaData> in the inheritance chain
        var baseType = sagaType.BaseType;
        while (baseType != null)
        {
            if (baseType is { IsGenericType: true, Name: "Saga", TypeArguments.Length: 1 })
            {
                if (baseType.TypeArguments[0] is INamedTypeSymbol sagaDataType)
                {
                    return sagaDataType;
                }
            }
            baseType = baseType.BaseType;
        }
        return null;
    }

    static string CreateMethodName(INamedTypeSymbol sagaType)
    {
        const string NamePrefix = "AddSaga_";

        var sb = new StringBuilder(NamePrefix, 50)
            .Append(sagaType.Name)
            .Append('_');

        var sagaFullName = sagaType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var hash = NonCryptographicHash.GetHash(sagaFullName);

        sb.Append(hash.ToString("x16"));

        return sb.ToString();
    }

    static EquatableArray<PropertyMappingInfo> ExtractPropertyMappings(
        INamedTypeSymbol sagaType,
        INamedTypeSymbol sagaDataType,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var mappings = ImmutableArray.CreateBuilder<PropertyMappingInfo>();
        var configureMethod = FindConfigureHowToFindSagaMethod(sagaType, cancellationToken);

        if (configureMethod == null)
        {
            return mappings.ToImmutable();
        }

        // Get syntax node from method symbol
        // Sort syntax references by file path to ensure deterministic selection
        var syntaxRefs = configureMethod.DeclaringSyntaxReferences
            .OrderBy(r => r.SyntaxTree.FilePath, StringComparer.Ordinal)
            .ThenBy(r => r.Span.Start)
            .ToArray();
        if (syntaxRefs.Length == 0)
        {
            return mappings.ToImmutable();
        }

        var methodSyntax = syntaxRefs[0].GetSyntax(cancellationToken);
        if (methodSyntax is not MethodDeclarationSyntax methodDeclaration)
        {
            return mappings.ToImmutable();
        }

        // Get method body (block or expression body)
        SyntaxNode? methodBody = methodDeclaration.Body ?? (SyntaxNode?)methodDeclaration.ExpressionBody?.Expression;
        if (methodBody == null)
        {
            return mappings.ToImmutable();
        }

        var walker = new ConfigureMappingWalker(semanticModel, sagaDataType, cancellationToken);
        walker.Visit(methodBody);

        // Sort mappings to ensure deterministic ordering
        var sortedMappings = walker.PropertyMappings.OrderBy(m => m.MessageType, StringComparer.Ordinal).ToImmutableArray();
        return sortedMappings;
    }

    static EquatableArray<HeaderMappingInfo> ExtractHeaderMappings(
        INamedTypeSymbol sagaType,
        INamedTypeSymbol sagaDataType,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var mappings = ImmutableArray.CreateBuilder<HeaderMappingInfo>();
        var configureMethod = FindConfigureHowToFindSagaMethod(sagaType, cancellationToken);

        if (configureMethod == null)
        {
            return mappings.ToImmutable();
        }

        // Get syntax node from method symbol
        // Sort syntax references by file path to ensure deterministic selection
        var syntaxRefs = configureMethod.DeclaringSyntaxReferences
            .OrderBy(r => r.SyntaxTree.FilePath, StringComparer.Ordinal)
            .ThenBy(r => r.Span.Start)
            .ToArray();
        if (syntaxRefs.Length == 0)
        {
            return mappings.ToImmutable();
        }

        var methodSyntax = syntaxRefs[0].GetSyntax(cancellationToken);
        if (methodSyntax is not MethodDeclarationSyntax methodDeclaration)
        {
            return mappings.ToImmutable();
        }

        // Get method body (block or expression body)
        SyntaxNode? methodBody = methodDeclaration.Body ?? (SyntaxNode?)methodDeclaration.ExpressionBody?.Expression;
        if (methodBody == null)
        {
            return mappings.ToImmutable();
        }

        var walker = new ConfigureMappingWalker(semanticModel, sagaDataType, cancellationToken);
        walker.Visit(methodBody);

        // Sort mappings to ensure deterministic ordering
        var sortedMappings = walker.HeaderMappings.OrderBy(m => m.MessageType, StringComparer.Ordinal).ToImmutableArray();
        return sortedMappings;
    }

    static EquatableArray<MessageInfo> ExtractMessageTypes(
        INamedTypeSymbol sagaType,
        CancellationToken cancellationToken)
    {
        var messages = ImmutableArray.CreateBuilder<MessageInfo>();
        var addedMessageTypes = new HashSet<string>();

        // Sort interfaces to ensure deterministic ordering
        var interfaces = sagaType.Interfaces.OrderBy(i => i.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal);

        // Find IAmStartedByMessages<T> interfaces
        foreach (var interfaceType in interfaces)
        {
            if (interfaceType.IsGenericType &&
                interfaceType.OriginalDefinition.Name == "IAmStartedByMessages" &&
                interfaceType.OriginalDefinition.ContainingNamespace.Name == "NServiceBus" &&
                interfaceType.OriginalDefinition.ContainingNamespace.ContainingNamespace.IsGlobalNamespace)
            {
                if (interfaceType.TypeArguments.Length == 1 && interfaceType.TypeArguments[0] is INamedTypeSymbol messageType)
                {
                    var messageTypeName = messageType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    if (addedMessageTypes.Add(messageTypeName))
                    {
                        messages.Add(new MessageInfo(messageTypeName, CanStartSaga: true));
                    }
                }
            }
        }

        // Find IHandleMessages<T> interfaces
        foreach (var interfaceType in interfaces)
        {
            if (interfaceType.IsGenericType &&
                interfaceType.OriginalDefinition.Name == "IHandleMessages" &&
                interfaceType.OriginalDefinition.ContainingNamespace.Name == "NServiceBus" &&
                interfaceType.OriginalDefinition.ContainingNamespace.ContainingNamespace.IsGlobalNamespace)
            {
                if (interfaceType.TypeArguments.Length == 1 && interfaceType.TypeArguments[0] is INamedTypeSymbol messageType)
                {
                    var messageTypeName = messageType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    // Only add if not already added as startable
                    if (addedMessageTypes.Add(messageTypeName))
                    {
                        messages.Add(new MessageInfo(messageTypeName, CanStartSaga: false));
                    }
                }
            }
        }

        // Find IHandleTimeouts<T> interfaces
        foreach (var interfaceType in interfaces)
        {
            if (interfaceType.IsGenericType &&
                interfaceType.OriginalDefinition.Name == "IHandleTimeouts" &&
                interfaceType.OriginalDefinition.ContainingNamespace.Name == "NServiceBus" &&
                interfaceType.OriginalDefinition.ContainingNamespace.ContainingNamespace.IsGlobalNamespace)
            {
                if (interfaceType.TypeArguments.Length == 1 && interfaceType.TypeArguments[0] is INamedTypeSymbol messageType)
                {
                    var messageTypeName = messageType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    // Only add if not already added
                    if (addedMessageTypes.Add(messageTypeName))
                    {
                        messages.Add(new MessageInfo(messageTypeName, CanStartSaga: false));
                    }
                }
            }
        }

        // Sort messages to ensure deterministic ordering (startable messages first, then by type name)
        return messages.OrderByDescending(m => m.CanStartSaga)
            .ThenBy(m => m.MessageType, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    static IMethodSymbol? FindConfigureHowToFindSagaMethod(
        INamedTypeSymbol sagaType,
        CancellationToken cancellationToken)
    {
        // Look for protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TSagaData> mapper)
        foreach (var member in sagaType.GetMembers("ConfigureHowToFindSaga"))
        {
            if (member is IMethodSymbol method &&
                method.IsOverride &&
                method.DeclaredAccessibility == Accessibility.Protected &&
                method.Parameters.Length == 1)
            {
                return method;
            }
        }

        return null;
    }

    static bool IsAddSagaMethod(IMethodSymbol method) => method is
    {
        Name: AddSagaMethodName,
        IsGenericMethod: true,
        TypeArguments.Length: 1,
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

        var groups = intercepts.GroupBy(i => i.MethodName)
            .OrderBy(g => g.Key, StringComparer.Ordinal);
        foreach (IGrouping<string, InterceptDetails> group in groups)
        {
            foreach (InterceptDetails location in group)
            {
                sb.AppendLine($"        {location.Location.Attribute} // {location.Location.DisplayLocation}");
            }

            InterceptDetails first = group.First();

            // Generate builder API calls
            var builderCode = GenerateBuilderCode(first);

            sb.AppendLine($$"""
                                    public static void {{first.MethodName}}(this NServiceBus.EndpointConfiguration endpointConfiguration)
                                    {
                                        System.ArgumentNullException.ThrowIfNull(endpointConfiguration);
                                        
                                        var sagaMetadataCollection = NServiceBus.Configuration.AdvancedExtensibility.AdvancedExtensibilityExtensions.GetSettings(endpointConfiguration)
                                            .GetOrCreate<NServiceBus.Sagas.SagaMetadataCollection>();
                                        
                                        {{builderCode}}
                                        sagaMetadataCollection.Register(metadata);
                                    }
                            """);
        }

        sb.Append("""
                          }
                      }
                      """);
        sb.AppendLine();
        sb.AppendLine();

        context.AddSource("InterceptionsOfAddSagaMethod.g.cs", sb.ToString());
    }

    static string GenerateBuilderCode(InterceptDetails details)
    {
        var sb = new StringBuilder();
        sb.Append("var metadata = NServiceBus.Sagas.SagaMetadataBuilder.Register<");
        sb.Append(details.SagaType);
        sb.Append(", ");
        sb.Append(details.SagaDataType);
        sb.AppendLine(">()");

        // Add property mappings
        foreach (var mapping in (ImmutableArray<PropertyMappingInfo>)details.PropertyMappings)
        {
            sb.Append("            .WithPropertyMapping<");
            sb.Append(mapping.MessageType);
            sb.Append(">(\"");
            sb.Append(mapping.SagaPropertyName);
            sb.Append("\", typeof(");
            sb.Append(mapping.SagaPropertyType);
            sb.Append("), \"");
            sb.Append(mapping.MessagePropertyName);
            sb.AppendLine("\")");
        }

        // Add header mappings
        foreach (var mapping in (ImmutableArray<HeaderMappingInfo>)details.HeaderMappings)
        {
            sb.Append("            .WithHeaderMapping<");
            sb.Append(mapping.MessageType);
            sb.Append(">(\"");
            sb.Append(mapping.SagaPropertyName);
            sb.Append("\", typeof(");
            sb.Append(mapping.SagaPropertyType);
            sb.Append("), \"");
            sb.Append(mapping.HeaderName);
            sb.AppendLine("\")");
        }

        // Add messages
        foreach (var message in (ImmutableArray<MessageInfo>)details.Messages)
        {
            sb.Append("            .WithMessage<");
            sb.Append(message.MessageType);
            sb.Append(">(");
            sb.Append(message.CanStartSaga ? "true" : "false");
            sb.AppendLine(")");
        }

        sb.Append("            .Build();");

        return sb.ToString();
    }

    const string AddSagaClassName = "SagaRegistrationExtensions";
    const string AddSagaMethodName = "AddSaga";

    record InterceptDetails(
        SafeInterceptionLocation Location,
        string MethodName,
        string SagaType,
        string SagaDataType,
        EquatableArray<PropertyMappingInfo> PropertyMappings,
        EquatableArray<HeaderMappingInfo> HeaderMappings,
        EquatableArray<MessageInfo> Messages);

    record PropertyMappingInfo(string MessageType, string SagaPropertyName, string SagaPropertyType, string MessagePropertyName);
    record HeaderMappingInfo(string MessageType, string SagaPropertyName, string SagaPropertyType, string HeaderName);
    record MessageInfo(string MessageType, bool CanStartSaga);
    readonly record struct SafeInterceptionLocation(string Attribute, string DisplayLocation)
    {
        public static SafeInterceptionLocation From(InterceptableLocation location) =>
            new(location.GetInterceptsLocationAttributeSyntax(), location.GetDisplayLocation());
    }

    class ConfigureMappingWalker : CSharpSyntaxWalker
    {
        readonly SemanticModel semanticModel;
        readonly INamedTypeSymbol sagaDataType;
        readonly CancellationToken cancellationToken;
        readonly List<PropertyMappingInfo> propertyMappings = [];
        readonly List<HeaderMappingInfo> headerMappings = [];

        public ConfigureMappingWalker(SemanticModel semanticModel, INamedTypeSymbol sagaDataType, CancellationToken cancellationToken)
        {
            this.semanticModel = semanticModel;
            this.sagaDataType = sagaDataType;
            this.cancellationToken = cancellationToken;
        }

        public ImmutableArray<PropertyMappingInfo> PropertyMappings => propertyMappings.ToImmutableArray();
        public ImmutableArray<HeaderMappingInfo> HeaderMappings => headerMappings.ToImmutableArray();

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            base.VisitInvocationExpression(node);

            // Look for .ToSaga(...) calls
            if (node.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name.Identifier.ValueText == "ToSaga")
            {
                // This is a ToSaga call, now we need to find the ConfigureMapping or ConfigureHeaderMapping call
                if (memberAccess.Expression is InvocationExpressionSyntax configureMappingCall)
                {
                    AnalyzeConfigureMappingCall(configureMappingCall, node);
                }
            }

            // Look for .ToMessage<TMessage>(...) calls (from MapSaga syntax)
            if (node.Expression is MemberAccessExpressionSyntax toMessageAccess &&
                toMessageAccess.Name is GenericNameSyntax genericName &&
                genericName.Identifier.ValueText == "ToMessage")
            {
                // This is a ToMessage call from MapSaga().ToMessage<TMessage>(...)
                // The pattern is: mapper.MapSaga(saga => saga.Prop).ToMessage<TMessage>(msg => msg.Prop)
                // This internally calls ConfigureMapping(...).ToSaga(...), so we need to trace back
                AnalyzeMapSagaToMessageCall(node, toMessageAccess);
            }
        }

        void AnalyzeMapSagaToMessageCall(InvocationExpressionSyntax toMessageCall, MemberAccessExpressionSyntax toMessageAccess)
        {
            // The expression structure is: [MapSaga call].ToMessage<TMessage>(...)
            // We need to find the MapSaga call and extract the saga property
            if (toMessageAccess.Expression is InvocationExpressionSyntax mapSagaCall)
            {
                // Extract saga property from MapSaga argument
                if (mapSagaCall.ArgumentList.Arguments.Count > 0)
                {
                    var sagaPropertyArg = mapSagaCall.ArgumentList.Arguments[0].Expression;
                    var sagaPropertyInfo = ExtractPropertyInfoFromExpression(sagaPropertyArg, sagaDataType);

                    if (sagaPropertyInfo != null && toMessageCall.ArgumentList.Arguments.Count > 0)
                    {
                        // Extract message property and type
                        var messagePropertyArg = toMessageCall.ArgumentList.Arguments[0].Expression;
                        var messagePropertyName = ExtractPropertyNameFromExpression(messagePropertyArg);

                        // Get message type from generic argument
                        if (toMessageAccess.Name is GenericNameSyntax genericName &&
                            genericName.TypeArgumentList.Arguments.Count > 0)
                        {
                            var messageTypeSyntax = genericName.TypeArgumentList.Arguments[0];

                            if (semanticModel.GetSymbolInfo(messageTypeSyntax, cancellationToken).Symbol is INamedTypeSymbol messageTypeSymbol && messagePropertyName != null)
                            {
                                var messageTypeName = messageTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                                propertyMappings.Add(new PropertyMappingInfo(
                                    messageTypeName,
                                    sagaPropertyInfo.Name,
                                    sagaPropertyInfo.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                    messagePropertyName));
                            }
                        }
                    }
                }
            }
        }

        void AnalyzeConfigureMappingCall(InvocationExpressionSyntax configureMappingCall, InvocationExpressionSyntax toSagaCall)
        {
            if (semanticModel.GetOperation(configureMappingCall, cancellationToken) is not IInvocationOperation configureOp)
            {
                return;
            }

            var method = configureOp.TargetMethod;
            var methodName = method.Name;

            // Check if this is ConfigureMapping<TMessage>(...)
            if (methodName == "ConfigureMapping" && method.IsGenericMethod && method.TypeArguments.Length == 1)
            {
                var messageType = method.TypeArguments[0];
                if (messageType is INamedTypeSymbol messageTypeSymbol)
                {
                    // Extract message property from ConfigureMapping argument
                    if (configureMappingCall.ArgumentList.Arguments.Count > 0)
                    {
                        var messagePropertyArg = configureMappingCall.ArgumentList.Arguments[0].Expression;
                        var messagePropertyName = ExtractPropertyNameFromExpression(messagePropertyArg);

                        // Extract saga property from ToSaga argument
                        if (toSagaCall.ArgumentList.Arguments.Count > 0)
                        {
                            var sagaPropertyArg = toSagaCall.ArgumentList.Arguments[0].Expression;
                            var sagaPropertyInfo = ExtractPropertyInfoFromExpression(sagaPropertyArg, sagaDataType);

                            if (messagePropertyName != null && sagaPropertyInfo != null)
                            {
                                var messageTypeName = messageTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                                propertyMappings.Add(new PropertyMappingInfo(
                                    messageTypeName,
                                    sagaPropertyInfo.Name,
                                    sagaPropertyInfo.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                    messagePropertyName));
                            }
                        }
                    }
                }
            }
            // Check if this is ConfigureHeaderMapping<TMessage>(...)
            else if (methodName == "ConfigureHeaderMapping" && method.IsGenericMethod && method.TypeArguments.Length == 1)
            {
                var messageType = method.TypeArguments[0];
                if (messageType is INamedTypeSymbol messageTypeSymbol)
                {
                    // Extract header name from ConfigureHeaderMapping argument
                    string? headerName = null;
                    if (configureMappingCall.ArgumentList.Arguments.Count > 0)
                    {
                        var headerArg = configureMappingCall.ArgumentList.Arguments[0].Expression;
                        if (headerArg is LiteralExpressionSyntax literal && literal.Kind() == SyntaxKind.StringLiteralExpression)
                        {
                            headerName = literal.Token.ValueText;
                        }
                    }

                    // Extract saga property from ToSaga argument
                    if (toSagaCall.ArgumentList.Arguments.Count > 0 && headerName != null)
                    {
                        var sagaPropertyArg = toSagaCall.ArgumentList.Arguments[0].Expression;
                        var sagaPropertyInfo = ExtractPropertyInfoFromExpression(sagaPropertyArg, sagaDataType);

                        if (sagaPropertyInfo != null)
                        {
                            var messageTypeName = messageTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            headerMappings.Add(new HeaderMappingInfo(
                                messageTypeName,
                                sagaPropertyInfo.Name,
                                sagaPropertyInfo.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                headerName));
                        }
                    }
                }
            }
        }

        static string? ExtractPropertyNameFromExpression(ExpressionSyntax expression)
        {
            // Handle lambda: message => message.Property
            if (expression is LambdaExpressionSyntax lambda)
            {
                if (lambda.Body is MemberAccessExpressionSyntax memberAccess)
                {
                    return memberAccess.Name.Identifier.ValueText;
                }
                // Handle conversion: message => (object)message.Property
                if (lambda.Body is CastExpressionSyntax cast && cast.Expression is MemberAccessExpressionSyntax castMember)
                {
                    return castMember.Name.Identifier.ValueText;
                }
            }

            return null;
        }

        IPropertySymbol? ExtractPropertyInfoFromExpression(ExpressionSyntax expression, INamedTypeSymbol sagaDataType)
        {
            // Handle lambda: saga => saga.Property
            if (expression is LambdaExpressionSyntax lambda)
            {
                var body = lambda.Body;

                // Handle conversion: saga => (object)saga.Property
                if (body is CastExpressionSyntax cast)
                {
                    body = cast.Expression;
                }

                if (body is MemberAccessExpressionSyntax memberAccess)
                {
                    if (semanticModel.GetSymbolInfo(memberAccess, cancellationToken).Symbol is IPropertySymbol property)
                    {
                        // Verify the property belongs to the saga data type
                        if (property.ContainingType.Equals(sagaDataType, SymbolEqualityComparer.Default))
                        {
                            return property;
                        }
                    }
                }
            }

            return null;
        }
    }
}