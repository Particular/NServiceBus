namespace NServiceBus.Core.Analyzer
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ForwardCancellationTokenAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "NSB0002";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            ForwardCancellationTokenDiagnostic
        );

        public override void Initialize(AnalysisContext context) =>
            context.WithDefaultSettings().RegisterCompilationStartAction(AnalyzeCompilationStart);

        static void AnalyzeCompilationStart(CompilationStartAnalysisContext startContext)
        {
            var knownTypes = new KnownTypes(startContext.Compilation);
            if (knownTypes.IsValid)
            {
                startContext.RegisterSyntaxNodeAction(context => AnalyzeMethod(context, knownTypes), SyntaxKind.MethodDeclaration);
            }
        }

        class KnownTypes
        {
            public INamedTypeSymbol CancellationTokenType { get; }
            public INamedTypeSymbol CancellableContextType { get; }

            public bool IsValid => CancellationTokenType != null && CancellableContextType != null;

            public KnownTypes(Compilation compilation)
            {
                CancellationTokenType = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
                CancellableContextType = compilation.GetTypeByMetadataName("NServiceBus.ICancellableContext");
            }
        }

        static void AnalyzeMethod(SyntaxNodeAnalysisContext context, KnownTypes knownTypes)
        {
            if (!(context.Node is MethodDeclarationSyntax parentMethodSyntax))
            {
                return;
            }

            if (!(context.SemanticModel.GetDeclaredSymbol(parentMethodSyntax) is IMethodSymbol parentMethod))
            {
                return;
            }

            if (!(parentMethod.Parameters.FirstOrDefault(param => IsCancellableContext(param.Type, knownTypes)) is IParameterSymbol contextParam))
            {
                return;
            }

            foreach (var invocation in parentMethodSyntax.Body.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                AnalyzeInvocation(context, invocation, contextParam, knownTypes);
            }
        }

        static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation, IParameterSymbol contextParam, KnownTypes knownTypes)
        {
            var args = invocation.ArgumentList.ChildNodes().OfType<ArgumentSyntax>().ToImmutableArray();

            if (args.Any(arg => IsCancellationToken(arg, contextParam.Name, knownTypes.CancellationTokenType, context)))
            {
                return;
            }

            if (!(context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is IMethodSymbol calledMethod))
            {
                return;
            }

            if (HasACancellationTokenParameter(calledMethod, knownTypes, args, out var explicitParamName))
            {
                ReportDiagnostic(context, invocation, contextParam.Name, calledMethod, explicitParamName);
                return;
            }

            // get the type containing the method being called
            var calledType = calledMethod.ContainingType;

            if (invocation.ChildNodes().OfType<MemberAccessExpressionSyntax>().FirstOrDefault() is MemberAccessExpressionSyntax memberAccess)
            {
                var memberAccessIdentifier = memberAccess.ChildNodes().OfType<IdentifierNameSyntax>().FirstOrDefault();

                if (memberAccessIdentifier != null)
                {
                    var memberAccessSymbolInfo = context.SemanticModel.GetSymbolInfo(memberAccessIdentifier, context.CancellationToken);
                    if (memberAccessSymbolInfo.Symbol is ILocalSymbol localSymbol && localSymbol.Type is INamedTypeSymbol memberExpressionInstanceType)
                    {
                        calledType = memberExpressionInstanceType;
                    }
                }
            }

            // walk the type ancestors and look for a method with a token
            for (; calledType != null && calledType.Name != "System.Object"; calledType = calledType.BaseType)
            {
                if (HasOverloadWithCancellationToken(calledType, calledMethod))
                {
                    ReportDiagnostic(context, invocation, contextParam.Name, calledMethod);
                    return;
                }
            }
        }

        static bool IsCancellableContext(ITypeSymbol type, KnownTypes knownTypes) =>
            type.Equals(knownTypes.CancellableContextType) ||
            type.AllInterfaces.Any(@interface => @interface.Equals(knownTypes.CancellableContextType));

        static bool HasACancellationTokenParameter(IMethodSymbol method, KnownTypes knownTypes, ImmutableArray<ArgumentSyntax> arguments, out string explicitParamName)
        {
            explicitParamName = null;

            // If is an NServiceBus-namespace extension method that extends IMessageHandlerContext, then skip
            if (method.IsExtensionMethod && method.ContainingNamespace?.Name == "NServiceBus")
            {
                if (method.ReducedFrom is IMethodSymbol reducedFromExtensionMethodDefinition)
                {
                    var thisParam = reducedFromExtensionMethodDefinition.Parameters.FirstOrDefault();
                    if (thisParam != null && thisParam.Type.Equals(knownTypes.CancellableContextType))
                    {
                        return false;
                    }
                }
            }

            if (method.Parameters.IsEmpty || !(method.Parameters[method.Parameters.Length - 1] is IParameterSymbol lastParameter))
            {
                return false;
            }

            // If parameter has a default value being used
            if (lastParameter.Type.Equals(knownTypes.CancellationTokenType) && lastParameter.IsOptional)
            {
                if (method.Parameters.Length != arguments.Length + 1)
                {
                    explicitParamName = lastParameter.Name;
                }
                return true;
            }

            return false;
        }

        static void ReportDiagnostic(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation, string contextParamName, IMethodSymbol methodSymbol, string explicitParamName = null)
        {
            var properties = new Dictionary<string, string>
            {
                { "ContextParamName", contextParamName },
                { "MethodName", methodSymbol.Name },
                { "ExplicitParamName", explicitParamName }
            }.ToImmutableDictionary();

            var diagnostic = Diagnostic.Create(ForwardCancellationTokenDiagnostic, invocation.GetLocation(), properties, contextParamName, methodSymbol.Name);

            context.ReportDiagnostic(diagnostic);
        }

        static bool HasOverloadWithCancellationToken(INamedTypeSymbol type, IMethodSymbol method)
        {
            var candidates = type.GetMembers(method.Name)
                .OfType<IMethodSymbol>()
                .Where(candidate => candidate.Parameters.LastOrDefault()?.Type.Name == "CancellationToken");

            foreach (var candidate in candidates)
            {
                if (MethodIsMatch(candidate, method))
                {
                    return true;
                }
            }

            return false;
        }

        static bool MethodIsMatch(IMethodSymbol alternate, IMethodSymbol current)
        {
            if (alternate == current)
            {
                // This means it's the same method, but the CancellationToken at the end is an optional parameter
                return true;
            }

            if (alternate.Parameters.Length != current.Parameters.Length + 1)
            {
                return false;
            }

            for (int i = 0; i < current.Parameters.Length; i++)
            {
                if (alternate.Parameters[i].Type != current.Parameters[i].Type)
                {
                    return false;
                }
            }

            return true;
        }

        static bool IsCancellationToken(ArgumentSyntax arg, string contextParamName, INamedTypeSymbol cancellationTokenType, SyntaxNodeAnalysisContext context)
        {
            if (arg.Expression is LiteralExpressionSyntax)
            {
                // Values like true, 3, 'x' are not cancellation tokens
                return false;
            }

            if (arg.Expression is ObjectCreationExpressionSyntax objectCreation &&
                objectCreation.Type is IdentifierNameSyntax objectCreationTypeName &&
                objectCreationTypeName.Identifier.ValueText == "CancellationToken")
            {
                return true;
            }

            if (arg.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                if (memberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression) &&
                    memberAccess.Expression is IdentifierNameSyntax exprId)
                {
                    var parent = exprId.Identifier.ValueText;
                    var member = memberAccess.Name.Identifier.ValueText;

                    // Is context.CancellationToken
                    if (parent == contextParamName && member == "CancellationToken")
                    {
                        return true;
                    }

                    if (parent == "CancellationToken" && member == "None")
                    {
                        return true;
                    }
                }
            }

            var expressionSymbol = context.SemanticModel.GetSymbolInfo(arg.Expression, context.CancellationToken);

            switch (expressionSymbol.Symbol)
            {
                case IFieldSymbol fieldSymbol:
                    return fieldSymbol.Type.Equals(cancellationTokenType);
                case IPropertySymbol propertySymbol:
                    return propertySymbol.Type.Equals(cancellationTokenType);
                case IMethodSymbol methodSymbol:
                    return methodSymbol.ReturnType.Equals(cancellationTokenType);
                case ILocalSymbol localSymbol:
                    return localSymbol.Type.Equals(cancellationTokenType);
                default:
                    return false;
            }
        }

        // {0} = IMessageHandlerContext parameter name
        // {1} = Name of method being invoked
        static readonly DiagnosticDescriptor ForwardCancellationTokenDiagnostic = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Forward `context.CancellationToken` to methods",
            messageFormat: "Forward `{0}.CancellationToken` to the `{1}` method.",
            category: "NServiceBus.Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Forward the `CancellationToken` from the context parameter to methods that take one to ensure the operation cancellation notifications are properly propagated. This ensures that message processing can be halted cleanly if necessary. Consider elevating the severity of \"CA2016: Forward the CancellationToken parameter to methods that take one\" to `warning` to ensure the token is passed correctly to other methods as well.");
    }
}
