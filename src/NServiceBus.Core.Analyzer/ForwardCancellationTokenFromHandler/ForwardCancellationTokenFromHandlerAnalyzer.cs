namespace NServiceBus.Core.Analyzer
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [Shared]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ForwardCancellationTokenFromHandlerAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        ///     Gets the list of supported diagnostics for the analyzer.
        /// </summary>
        /// <value>
        ///     The supported diagnostics.
        /// </value>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            ForwardCancellationTokenFromHandlerDiagnostic
        );

        /// <summary>
        ///     Initializes the specified analyzer on the <paramref name="context" />.
        /// </summary>
        /// <param name="context">The context.</param>
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(AnalyzeCompilationStart);
        }

        static void AnalyzeCompilationStart(CompilationStartAnalysisContext startContext)
        {
            var knownTypes = new KnownTypes(startContext.Compilation);
            if (knownTypes.IsValid)
            {
                startContext.RegisterSyntaxNodeAction(context => AnalyzeSyntax(context, knownTypes), SyntaxKind.InvocationExpression);
            }
        }

        class KnownTypes
        {
            public INamedTypeSymbol CancellationToken { get; }
            public INamedTypeSymbol IPipelineContext { get; }

            public bool IsValid => CancellationToken != null && IPipelineContext != null;

            public KnownTypes(Compilation compilation)
            {
                CancellationToken = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
                IPipelineContext = compilation.GetTypeByMetadataName("NServiceBus.IPipelineContext");
            }
        }

        static void AnalyzeSyntax(SyntaxNodeAnalysisContext context, KnownTypes knownTypes)
        {
            if (!(context.Node is InvocationExpressionSyntax invocation))
            {
                return;
            }

            // Get the parent method and make sure it's named "Handle"
            var handleMethodDeclaration = GetParent<MethodDeclarationSyntax>(invocation);
            if (handleMethodDeclaration == null || handleMethodDeclaration.Identifier.ValueText != "Handle")
            {
                return;
            }

            // Make sure the Handle method has a 2 parameters, the 2nd of which has a type named IMessageHandlerContext
            var handleParams = handleMethodDeclaration.ParameterList.ChildNodes().OfType<ParameterSyntax>().ToArray();
            if (handleParams.Length != 2 || !(handleParams[1].Type is IdentifierNameSyntax contextTypeIdentifier) || contextTypeIdentifier.Identifier.ValueText != "IMessageHandlerContext")
            {
                return;
            }

            var contextParamName = handleParams[1].Identifier.ValueText;

            // Get the owner class declaration and make sure that it has a base list, i.e. that it's possible for it to include one of the IHandle... interfaces in its ancestry
            var classDeclaration = GetParent<ClassDeclarationSyntax>(handleMethodDeclaration);
            if (classDeclaration == null || classDeclaration.BaseList == null)
            {
                return;
            }

            var invocationArgs = invocation.ArgumentList.ChildNodes().OfType<ArgumentSyntax>().ToArray();
            if (invocationArgs.Any(arg => ArgumentIsACancellationToken(context, arg, contextParamName, knownTypes)))
            {
                return;
            }

            // Check cancellation before getting expensive symbol info
            if (context.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (!(context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is IMethodSymbol methodSymbol))
            {
                return;
            }

            ////var arguments = invocation.ArgumentList.Arguments.Select(arg => context.SemanticModel.GetSymbolInfo(arg, context.CancellationToken));

            if (InvocationMethodTakesAToken(methodSymbol, knownTypes))
            {
                ReportDiagnostic(context, invocation, contextParamName, methodSymbol);
                return;
            }

            var namedType = methodSymbol.ContainingType;
            var memberAccessExpr = invocation.ChildNodes().OfType<MemberAccessExpressionSyntax>().FirstOrDefault();
            if (memberAccessExpr != null)
            {
                var memberAccessIdentifier = memberAccessExpr.ChildNodes().OfType<IdentifierNameSyntax>().FirstOrDefault();
                if (memberAccessIdentifier != null)
                {
                    if (context.CancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var memberAccessSymbolInfo = context.SemanticModel.GetSymbolInfo(memberAccessIdentifier, context.CancellationToken);
                    if (memberAccessSymbolInfo.Symbol is ILocalSymbol localSymbol && localSymbol.Type is INamedTypeSymbol memberExpressionInstanceType)
                    {
                        namedType = memberExpressionInstanceType;
                    }
                }
            }

            for (var containingType = namedType; containingType != null && containingType.Name != "System.Object"; containingType = containingType.BaseType)
            {
                if (MethodHasOverloadWithCancellation(containingType, methodSymbol))
                {
                    ReportDiagnostic(context, invocation, contextParamName, methodSymbol);
                    return;
                }
            }
        }

        static bool InvocationMethodTakesAToken(IMethodSymbol method, KnownTypes knownTypes)
        {
            // If is an NServiceBus-namespace extension method that extends IMessageHandlerContext, then skip
            if (method.IsExtensionMethod && method.ContainingNamespace?.Name == "NServiceBus")
            {
                if (method.ReducedFrom is IMethodSymbol reducedFromExtensionMethodDefinition)
                {
                    var thisParam = reducedFromExtensionMethodDefinition.Parameters.FirstOrDefault();
                    if (thisParam != null && thisParam.Type.Equals(knownTypes.IPipelineContext))
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
            if (lastParameter.Type.Equals(knownTypes.CancellationToken) && lastParameter.IsOptional)
            {
                return true;
            }

            return false;
        }

        static void ReportDiagnostic(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation, string contextParamName, IMethodSymbol methodSymbol)
        {
            var properties = new Dictionary<string, string>
            {
                { "ContextParamName", contextParamName },
                { "MethodName", methodSymbol.Name }
            }.ToImmutableDictionary();

            var diagnostic = Diagnostic.Create(ForwardCancellationTokenFromHandlerDiagnostic, invocation.GetLocation(), properties, contextParamName, methodSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool MethodHasOverloadWithCancellation(INamedTypeSymbol type, IMethodSymbol currentMethod)
        {
            var possibilities = type.GetMembers(currentMethod.Name)
                .OfType<IMethodSymbol>()
                .Where(method => method.Parameters.LastOrDefault()?.Type.Name == "CancellationToken");

            if (possibilities.Any())
            {
                foreach (var alternateMethod in possibilities)
                {
                    if (MethodIsMatch(alternateMethod, currentMethod))
                    {
                        return true;
                    }
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static T GetParent<T>(SyntaxNode node) where T : SyntaxNode
        {
            if (node == null)
            {
                return null;
            }

            while (node != null && !(node is T))
            {
                node = node.Parent;
            }
            return node as T;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool ArgumentIsACancellationToken(SyntaxNodeAnalysisContext context, ArgumentSyntax arg, string contextParamName, KnownTypes types)
        {
            if (arg.Expression is LiteralExpressionSyntax)
            {
                // Values like true, 3, 'x' are not cancellation tokens
                return false;
            }

            if (arg.Expression is ObjectCreationExpressionSyntax objectCreation && objectCreation.Type is IdentifierNameSyntax creationId && creationId.Identifier.ValueText == "CancellationToken")
            {
                return true;
            }

            if (arg.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                if (memberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression) && memberAccess.Expression is IdentifierNameSyntax exprId)
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
                    return fieldSymbol.Type.Equals(types.CancellationToken);
                case IPropertySymbol propertySymbol:
                    return propertySymbol.Type.Equals(types.CancellationToken);
                case IMethodSymbol methodSymbol:
                    return methodSymbol.ReturnType.Equals(types.CancellationToken);
                case ILocalSymbol localSymbol:
                    return localSymbol.Type.Equals(types.CancellationToken);
                default:
                    return false;
            }
        }

        // {0} = Variable name for the IMessageHandlerContext parameter
        // {1} = Name of method being invoked
        internal static readonly DiagnosticDescriptor ForwardCancellationTokenFromHandlerDiagnostic = new DiagnosticDescriptor(
            id: "NSB0002",
            title: "Forward `IMessageHandlerContext.CancellationToken` to methods",
            messageFormat: "Forward `{0}.CancellationToken` to the `{1}` method.",
            category: "NServiceBus.Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Forward the `CancellationToken` from the IMessageHandlerContext to methods that take one to ensure the operation cancellation notifications are properly propagated. This ensures that message processing can be halted cleanly if necessary. Consider elevating the severity of \"CA2016: Forward the CancellationToken parameter to methods that take one\" to `warning` to ensure the token is passed correctly to other methods as well.");
    }
}
