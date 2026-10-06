#nullable enable

namespace NServiceBus.Core.Analyzer.Sagas;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NServiceBus.Core.Analyzer.Handlers;
using static NServiceBus.Core.Analyzer.Handlers.Handlers;
using BaseParser = AddHandlerAndSagasRegistrationGenerator.Parser;

public static partial class Sagas
{
    public readonly record struct SagaSpecs(ImmutableEquatableArray<SagaSpec> Sagas);

    public record SagaSpec : AddHandlerAndSagasRegistrationGenerator.Parser.BaseSpec
    {
        public SagaSpec(HandlerSpec handler, string sagaDataFullyQualifiedName, CorrelationPropertyMappingSpec? correlationProperty, ImmutableEquatableArray<PropertyMappingSpec> propertyMappings)
            : base(handler)
        {
            SagaDataFullyQualifiedName = sagaDataFullyQualifiedName;
            CorrelationPropertyMapping = correlationProperty;
            PropertyMappings = propertyMappings;
            Handler = handler;
        }

        public string SagaDataFullyQualifiedName { get; }

        public CorrelationPropertyMappingSpec? CorrelationPropertyMapping { get; }
        public ImmutableEquatableArray<PropertyMappingSpec> PropertyMappings { get; }
        public HandlerSpec Handler { get; }
    }

    public record PropertyMappingSpec(string MessageType, string MessageName, string MessagePropertyName, string MessagePropertyType, string? InterfaceGetterReceiverType, string? ExternGetterReceiverType, string? ExternGetterMethodName, bool UsesUpdatedMemorySafetyRules, string? AccessedMember, ImmutableEquatableArray<string> SuppressedDiagnosticIds);
    public readonly record struct CorrelationPropertyMappingSpec(string PropertyName, string PropertyType, string PropertyTypeMetadataName, string? ExternGetterReceiverType, string? ExternSetterReceiverType, bool UsesUpdatedMemorySafetyRules, ImmutableEquatableArray<string> SuppressedGetterDiagnosticIds, ImmutableEquatableArray<string> SuppressedSetterDiagnosticIds);

    public static class Parser
    {
        public static SagaSpec? Parse(SemanticModel semanticModel, INamedTypeSymbol sagaType, HandlerKnownTypes knownTypes, CancellationToken cancellationToken = default) => ParseCore(semanticModel.Compilation, sagaType, knownTypes, cancellationToken);

        static SagaSpec? ParseCore(Compilation compilation, INamedTypeSymbol sagaType, HandlerKnownTypes knownTypes, CancellationToken cancellationToken)
        {
            // Extract saga data type from Saga<TSagaData>
            var sagaDataType = GetSagaDataType(sagaType);
            if (sagaDataType == null)
            {
                return null;
            }

            if (sagaType.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree is not { } sagaSyntaxTree)
            {
                return null;
            }

            var sagaSemanticModel = compilation.GetSemanticModel(sagaSyntaxTree);

            var sagaBaseSpec = Handlers.Parser.Parse(sagaType, BaseParser.SpecKind.Saga, knownTypes, cancellationToken);
            var sagaDataFullyQualifiedName = sagaDataType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            // Analyze ConfigureHowToFindSaga to extract mappings. Finder-only sagas have no correlation property
            // and no property mappings but are still valid sagas that must be registered.
            var (correlationProperty, propertyMappings) = ExtractPropertyMappings(sagaType, sagaSemanticModel, cancellationToken);

            return new SagaSpec(sagaBaseSpec, sagaDataFullyQualifiedName, correlationProperty, propertyMappings);
        }

        static INamedTypeSymbol? GetSagaDataType(INamedTypeSymbol sagaType)
        {
            // Find Saga<TSagaData> in the inheritance chain
            var baseType = sagaType.BaseType;
            while (baseType != null)
            {
                if (baseType is { IsGenericType: true, Name: "Saga", TypeArguments: [INamedTypeSymbol sagaDataType] })
                {
                    return sagaDataType;
                }

                baseType = baseType.BaseType;
            }

            return null;
        }

        static (CorrelationPropertyMappingSpec?, ImmutableEquatableArray<PropertyMappingSpec>) ExtractPropertyMappings(
            INamedTypeSymbol sagaType,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var configureMethod = FindConfigureHowToFindSagaMethod(sagaType);

            // Get syntax node from method symbol (single declaration for overrides)
            var syntaxRef = configureMethod?.DeclaringSyntaxReferences.FirstOrDefault();

            var methodSyntax = syntaxRef?.GetSyntax(cancellationToken);
            if (methodSyntax is not MethodDeclarationSyntax methodDeclaration)
            {
                return (null, ImmutableEquatableArray<PropertyMappingSpec>.Empty);
            }

            // Get method body (block or expression body)
            SyntaxNode? methodBody = methodDeclaration.Body ?? (SyntaxNode?)methodDeclaration.ExpressionBody?.Expression;
            if (methodBody == null)
            {
                return (null, ImmutableEquatableArray<PropertyMappingSpec>.Empty);
            }

            var walker = new ConfigureMappingWalker(semanticModel, cancellationToken);
            walker.Visit(methodBody);

            if (walker.CorrelationPropertyMapping is null)
            {
                return (null, ImmutableEquatableArray<PropertyMappingSpec>.Empty);
            }

            // Sort mappings to ensure deterministic ordering
            walker.Mappings.Sort(static (a, b) => string.CompareOrdinal(a.MessageType, b.MessageType));
            return (walker.CorrelationPropertyMapping, walker.Mappings.ToImmutableEquatableArray());
        }

        static IMethodSymbol? FindConfigureHowToFindSagaMethod(
            INamedTypeSymbol sagaType)
        {
            // Look for protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TSagaData> mapper)
            foreach (var member in sagaType.GetMembers("ConfigureHowToFindSaga"))
            {
                if (member is IMethodSymbol { IsOverride: true, DeclaredAccessibility: Accessibility.Protected, Parameters.Length: 1 } method)
                {
                    return method;
                }
            }

            return null;
        }

        sealed class ConfigureMappingWalker(
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
            : CSharpSyntaxWalker
        {
            public List<PropertyMappingSpec> Mappings { get; } = [];
            public CorrelationPropertyMappingSpec? CorrelationPropertyMapping { get; private set; }

            public override void VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                base.VisitInvocationExpression(node);

                if (node.Expression is MemberAccessExpressionSyntax { Name: IdentifierNameSyntax { Identifier.ValueText: "MapSaga" } })
                {
                    // This is a MapSaga call from MapSaga().ToMessage<TMessage>(...)
                    // The pattern is: mapper.MapSaga(saga => saga.Prop).ToMessage<TMessage>(msg => msg.Prop)
                    AnalyzeToSagaCall(node);
                }

                // Look for .ToMessage<TMessage>(...) calls (from MapSaga syntax)
                if (node.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax { Identifier.ValueText: "ToMessage" } })
                {
                    // This is a ToMessage call from MapSaga().ToMessage<TMessage>(...)
                    // The pattern is: mapper.MapSaga(saga => saga.Prop).ToMessage<TMessage>(msg => msg.Prop)
                    AnalyzeMapSagaToMessageCall(node);
                }
            }

            void AnalyzeToSagaCall(InvocationExpressionSyntax mapSagaCall)
            {
                if (mapSagaCall.ArgumentList.Arguments.Count <= 0)
                {
                    return;
                }

                if (mapSagaCall.ArgumentList.Arguments[0].Expression is not LambdaExpressionSyntax lambda)
                {
                    return;
                }

                var memberAccess = TryGetMemberAccess(lambda.Body, cancellationToken);
                // SagaMapper rejects saga data mappings that don't access a property on the lambda parameter, so there's nothing to generate.
                if (memberAccess is null || !semanticModel.IsMemberAccessOnLambdaParameter(memberAccess, lambda, cancellationToken))
                {
                    return;
                }

                // Property name (syntax)
                var propertyName = memberAccess.Name.Identifier.ValueText;
                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    return;
                }

                var propertySymbol = ResolvePropertySymbol(semanticModel, memberAccess, cancellationToken);
                if (propertySymbol is null)
                {
                    return;
                }

                var propertyType = propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                // SagaMapper.AllowedCorrelationPropertyTypes only allows primitive types so
                // using the metadata name is enough to create meaningful accessor names without having to TitleCase things.
                string propertySymbolMetadataName = propertySymbol.Type.MetadataName;
                var (externGetter, suppressedGetterDiagnosticIds) = ResolveAccessor(propertySymbol, false);
                var (externSetter, suppressedSetterDiagnosticIds) = ResolveAccessor(propertySymbol, true);
                var needsExtern = externGetter is not null || externSetter is not null;
                CorrelationPropertyMapping = new CorrelationPropertyMappingSpec(propertyName, propertyType, propertySymbolMetadataName, ExternReceiverType(externGetter), ExternReceiverType(externSetter), needsExtern && semanticModel.UsesUpdatedMemorySafetyRules,
                    suppressedGetterDiagnosticIds, suppressedSetterDiagnosticIds);
            }

            void AnalyzeMapSagaToMessageCall(InvocationExpressionSyntax toMessageCall)
            {
                if (toMessageCall.ArgumentList.Arguments.Count <= 0)
                {
                    return;
                }

                if (toMessageCall.ArgumentList.Arguments[0].Expression is not LambdaExpressionSyntax lambda)
                {
                    return;
                }

                var memberAccess = TryGetMemberAccess(lambda.Body, cancellationToken);
                if (memberAccess is null)
                {
                    return;
                }

                // Property name (syntax)
                var propertyName = memberAccess.Name.Identifier.ValueText;
                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    return;
                }

                var propertySymbol = ResolvePropertySymbol(semanticModel, memberAccess, cancellationToken);
                if (propertySymbol is null)
                {
                    return;
                }

                // Message "variable" expression: the left side of "message.Property"
                var messageExpression = StripSyntaxWrappers(memberAccess.Expression, cancellationToken);

                // Message type (symbol)
                var messageTypeSymbol = semanticModel.GetTypeInfo(messageExpression, cancellationToken).Type ?? propertySymbol.ContainingType;
                if (messageTypeSymbol is null)
                {
                    return;
                }

                var messageType = messageTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var messageName = messageTypeSymbol.Name; // simple name, e.g. "SomeMessage"

                var propertyType = propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                // Without a member generated code can call, the runtime accessor compiled from the mapping expression is used.
                if (ResolveRead(propertySymbol, messageExpression) is not { } read)
                {
                    return;
                }

                Mappings.Add(new PropertyMappingSpec(messageType, messageName, propertyName, propertyType, read.InterfaceReceiverType, ExternReceiverType(read.ExternGetter), read.ExternGetter?.MetadataName,
                    read.ExternGetter is not null && semanticModel.UsesUpdatedMemorySafetyRules, read.AccessedMember, read.SuppressedDiagnosticIds));
            }

            // Reading through the interface dispatches like the mapping expression; when generated code can't call the interface getter, it falls back to the implementation on the receiver type.
            ReadAccess? ResolveRead(IPropertySymbol property, ExpressionSyntax receiverExpression)
            {
                if (property.ContainingType is not { TypeKind: TypeKind.Interface } declaringInterface)
                {
                    return DirectOrExternRead(property);
                }

                if (property.GetMethod is { } getter && IsAccessible(getter) && SuppressibleDiagnosticIds(property, false) is { } suppressedDiagnosticIds)
                {
                    var interfaceType = declaringInterface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    return new ReadAccess(interfaceType, null, $"{interfaceType}.{property.MetadataName}", suppressedDiagnosticIds);
                }

                if (ResolveImplementation(property, receiverExpression) is not ({ GetMethod: { } implementationGetter } implementation, var reachableByName))
                {
                    return null;
                }

                return reachableByName
                    ? DirectOrExternRead(implementation)
                    : new ReadAccess(null, implementationGetter, AccessedMember(implementation), ImmutableEquatableArray<string>.Empty);
            }

            ReadAccess DirectOrExternRead(IPropertySymbol property)
            {
                var (externGetter, suppressedDiagnosticIds) = ResolveAccessor(property, false);
                return new ReadAccess(null, externGetter, null, suppressedDiagnosticIds);
            }

            (IPropertySymbol Implementation, bool ReachableByName)? ResolveImplementation(IPropertySymbol property, ExpressionSyntax receiverExpression)
            {
                if (semanticModel.GetTypeInfo(receiverExpression, cancellationToken).Type is not { TypeKind: not TypeKind.Interface } receiver
                    || receiver.FindImplementationForInterfaceMember(property) is not IPropertySymbol { ContainingType.TypeKind: not TypeKind.Interface } implementation)
                {
                    return null;
                }

                var reachableByName = implementation.ExplicitInterfaceImplementations.IsEmpty
                    && semanticModel.LookupSymbols(receiverExpression.SpanStart, receiver, implementation.Name).Contains(implementation, SymbolEqualityComparer.Default);
                return (implementation, reachableByName);
            }

            static string AccessedMember(IPropertySymbol implementation) =>
                $"{implementation.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{implementation.MetadataName}";

            // Generated code doesn't see the mapping's suppressions, so it suppresses what the compiler reports for the accessor, or calls it through an extern when a pragma can't.
            (IMethodSymbol? ExternAccessor, ImmutableEquatableArray<string> SuppressedDiagnosticIds) ResolveAccessor(IPropertySymbol property, bool setter)
            {
                var accessor = OwnOrInheritedAccessor(property, setter);
                if (NeedsExtern(accessor, setter))
                {
                    return (accessor, ImmutableEquatableArray<string>.Empty);
                }

                return SuppressibleDiagnosticIds(property, setter) is { } suppressedDiagnosticIds
                    ? (null, suppressedDiagnosticIds)
                    : (accessor, ImmutableEquatableArray<string>.Empty);
            }

            static IMethodSymbol? OwnOrInheritedAccessor(IPropertySymbol? property, bool setter)
            {
                for (; property is not null; property = property.OverriddenProperty)
                {
                    if ((setter ? property.SetMethod : property.GetMethod) is { } accessor)
                    {
                        return accessor;
                    }
                }

                return null;
            }

            // Like the compiler, use the attributes of the member an override overrides, on both the property and the accessor. Null when a pragma can't suppress them.
            static ImmutableEquatableArray<string>? SuppressibleDiagnosticIds(IPropertySymbol property, bool setter)
            {
                while (property.OverriddenProperty is { } overridden)
                {
                    property = overridden;
                }

                SortedSet<string>? diagnosticIds = null;
                if (!TryAddSuppressibleDiagnosticIds(property, ref diagnosticIds)
                    || ((setter ? property.SetMethod : property.GetMethod) is { } accessor && !TryAddSuppressibleDiagnosticIds(accessor, ref diagnosticIds)))
                {
                    return null;
                }

                return diagnosticIds is null ? ImmutableEquatableArray<string>.Empty : diagnosticIds.ToImmutableEquatableArray();
            }

            static bool TryAddSuppressibleDiagnosticIds(ISymbol symbol, ref SortedSet<string>? diagnosticIds)
            {
                foreach (var attribute in symbol.GetAttributes())
                {
                    string? diagnosticId;
                    if (attribute.AttributeClass is { Name: "ObsoleteAttribute", ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } })
                    {
                        if (attribute.ConstructorArguments is [_, { Value: true }])
                        {
                            return false;
                        }

                        diagnosticId = attribute.NamedArguments.FirstOrDefault(static argument => argument.Key == "DiagnosticId").Value.Value as string;
                        // Without a custom ID the compiler reports CS0612 or CS0618, which generated files already suppress.
                        if (string.IsNullOrEmpty(diagnosticId))
                        {
                            continue;
                        }
                    }
                    else if (attribute.AttributeClass is { Name: "ExperimentalAttribute" } experimental && experimental.ContainingNamespace.ToDisplayString() == "System.Diagnostics.CodeAnalysis")
                    {
                        diagnosticId = attribute.ConstructorArguments is [{ Value: string experimentalId }] ? experimentalId : null;
                    }
                    else
                    {
                        continue;
                    }

                    if (diagnosticId is null || !IsPragmaIdentifier(diagnosticId))
                    {
                        return false;
                    }

                    (diagnosticIds ??= new SortedSet<string>(StringComparer.Ordinal)).Add(diagnosticId);
                }

                return true;
            }

            // A pragma matches by the identifier's value text, which drops formatting characters, and keywords like true don't parse as pragma codes.
            static bool IsPragmaIdentifier(string diagnosticId) =>
                SyntaxFactory.ParseLeadingTrivia($"#pragma warning disable {diagnosticId}") is [var trivia]
                && trivia.GetStructure() is PragmaWarningDirectiveTriviaSyntax { ErrorCodes: [IdentifierNameSyntax { Identifier.ValueText: var parsedId }], ContainsDiagnostics: false }
                && parsedId == diagnosticId;

            // Generated code can't call init-only or inaccessible accessors directly, so they go through an extern accessor on the declaring type.
            bool NeedsExtern(IMethodSymbol? accessor, bool initOnlyNeedsExtern) =>
                accessor is not null
                && ((initOnlyNeedsExtern && accessor.IsInitOnly) || (accessor.DeclaredAccessibility != Accessibility.Public && !IsAccessible(accessor)));

            bool IsAccessible(ISymbol symbol) => semanticModel.Compilation.IsSymbolAccessibleWithin(symbol, semanticModel.Compilation.Assembly);

            static string? ExternReceiverType(IMethodSymbol? accessor) => accessor?.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            readonly record struct ReadAccess(string? InterfaceReceiverType, IMethodSymbol? ExternGetter, string? AccessedMember, ImmutableEquatableArray<string> SuppressedDiagnosticIds);

            static MemberAccessExpressionSyntax? TryGetMemberAccess(SyntaxNode node, CancellationToken cancellationToken) =>
                node is ExpressionSyntax expression
                    ? StripSyntaxWrappers(expression, cancellationToken) as MemberAccessExpressionSyntax
                    : null;

            static ExpressionSyntax StripSyntaxWrappers(ExpressionSyntax expression, CancellationToken cancellationToken)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    switch (expression)
                    {
                        case CastExpressionSyntax cast:
                            expression = cast.Expression;
                            continue;
                        case ParenthesizedExpressionSyntax parenthesized:
                            expression = parenthesized.Expression;
                            continue;
                        case PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } suppressNullable:
                            expression = suppressNullable.Operand;
                            continue;
                        default:
                            return expression;
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
                return expression;
            }

            static IPropertySymbol? ResolvePropertySymbol(SemanticModel model, MemberAccessExpressionSyntax memberAccess, CancellationToken cancellationToken)
            {
                return AsProperty(model.GetSymbolInfo(memberAccess, cancellationToken))
                    ?? AsProperty(model.GetSymbolInfo(memberAccess.Name, cancellationToken));

                static IPropertySymbol? AsProperty(SymbolInfo symbolInfo) => symbolInfo.Symbol as IPropertySymbol
                        ?? symbolInfo.CandidateSymbols.OfType<IPropertySymbol>().FirstOrDefault();
            }
        }
    }
}