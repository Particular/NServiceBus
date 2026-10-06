#nullable enable

namespace NServiceBus.Core.Analyzer.Sagas;

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

    public record PropertyMappingSpec(string MessageType, string MessageName, string MessagePropertyName, string MessagePropertyType, string? InterfaceGetterReceiverType, string? ExternGetterReceiverType, string? ExternGetterMethodName, bool UsesUpdatedMemorySafetyRules, string? AccessedMember);
    public readonly record struct CorrelationPropertyMappingSpec(string PropertyName, string PropertyType, string PropertyTypeMetadataName, string? InterfaceGetterReceiverType, string? ExternGetterReceiverType, string? ExternGetterMethodName, string? InterfaceSetterReceiverType, string? ExternSetterReceiverType, string? ExternSetterMethodName, bool UsesUpdatedMemorySafetyRules, string? AccessedMember);

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

            if (!walker.MapsCorrelationProperty)
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
            public bool MapsCorrelationProperty { get; private set; }

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

                MapsCorrelationProperty = true;

                var receiverExpression = StripSyntaxWrappers(memberAccess.Expression, cancellationToken);
                if (ResolveRead(propertySymbol, receiverExpression) is not { } read)
                {
                    return;
                }

                var write = ResolveWrite(propertySymbol, receiverExpression);

                var propertyType = propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                // SagaMapper.AllowedCorrelationPropertyTypes only allows primitive types so
                // using the metadata name is enough to create meaningful accessor names without having to TitleCase things.
                string propertySymbolMetadataName = propertySymbol.Type.MetadataName;
                var needsExtern = read.ExternGetter is not null || write.ExternSetter is not null;
                CorrelationPropertyMapping = new CorrelationPropertyMappingSpec(propertyName, propertyType, propertySymbolMetadataName,
                    read.InterfaceReceiverType, ExternReceiverType(read.ExternGetter), read.ExternGetter?.MetadataName,
                    write.InterfaceReceiverType, ExternReceiverType(write.ExternSetter), write.ExternSetter?.MetadataName,
                    needsExtern && semanticModel.UsesUpdatedMemorySafetyRules(), read.AccessedMember);
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
                    read.ExternGetter is not null && semanticModel.UsesUpdatedMemorySafetyRules(), read.AccessedMember));
            }

            // Reading through the interface dispatches like the mapping expression; an inaccessible interface falls back to the implementation on the receiver type.
            ReadAccess? ResolveRead(IPropertySymbol property, ExpressionSyntax receiverExpression)
            {
                if (property.ContainingType is not { TypeKind: TypeKind.Interface } declaringInterface)
                {
                    return new ReadAccess(null, NeedsExtern(property.GetMethod, false) ? property.GetMethod : null, null);
                }

                if (property.GetMethod is { } getter && IsAccessible(getter))
                {
                    var interfaceType = declaringInterface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    return new ReadAccess(interfaceType, null, $"{interfaceType}.{property.MetadataName}");
                }

                if (ResolveImplementation(property, receiverExpression) is not ({ GetMethod: { } implementationGetter } implementation, var reachableByName))
                {
                    return null;
                }

                return reachableByName
                    ? new ReadAccess(null, NeedsExtern(implementationGetter, false) ? implementationGetter : null, null)
                    : new ReadAccess(null, implementationGetter, AccessedMember(implementation));
            }

            WriteAccess ResolveWrite(IPropertySymbol property, ExpressionSyntax receiverExpression)
            {
                if (property.ContainingType is { TypeKind: TypeKind.Interface }
                    && ResolveImplementation(property, receiverExpression) is ({ ExplicitInterfaceImplementations.IsEmpty: true } implementation, var reachableByName))
                {
                    return new WriteAccess(null, !reachableByName || NeedsExtern(implementation.SetMethod, true) ? implementation.SetMethod : null);
                }

                return new WriteAccess(null, NeedsExtern(property.SetMethod, true) ? property.SetMethod : null);
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

            // Generated code can't call init-only or inaccessible accessors directly, so they go through an extern accessor on the declaring type.
            bool NeedsExtern(IMethodSymbol? accessor, bool initOnlyNeedsExtern) =>
                accessor is not null
                && ((initOnlyNeedsExtern && accessor.IsInitOnly) || (accessor.DeclaredAccessibility != Accessibility.Public && !IsAccessible(accessor)));

            bool IsAccessible(ISymbol symbol) => semanticModel.Compilation.IsSymbolAccessibleWithin(symbol, semanticModel.Compilation.Assembly);

            static string? ExternReceiverType(IMethodSymbol? accessor) => accessor?.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            readonly record struct ReadAccess(string? InterfaceReceiverType, IMethodSymbol? ExternGetter, string? AccessedMember);

            readonly record struct WriteAccess(string? InterfaceReceiverType, IMethodSymbol? ExternSetter);

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