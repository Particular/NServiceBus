namespace NServiceBus.Core.Analyzer
{
    using System.Collections.Immutable;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

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
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression);
        }

        void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
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
            if (invocationArgs.Any(arg => ArgumentIsCancellationTokenFromContext(arg, contextParamName)))
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
                if (MethodGroupContainsOverloadWithCancellation(containingType, methodSymbol.Name))
                {
                    var diagnostic = Diagnostic.Create(ForwardCancellationTokenFromHandlerDiagnostic, invocation.GetLocation(), contextParamName, methodSymbol.Name);
                    context.ReportDiagnostic(diagnostic);
                    return;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool MethodGroupContainsOverloadWithCancellation(INamedTypeSymbol type, string methodName)
        {
            return type.GetMembers(methodName).OfType<IMethodSymbol>().Any(MethodSymbolHasCancellationParameter);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool MethodSymbolHasCancellationParameter(IMethodSymbol method)
        {
            return method.Parameters.Any(methodParam =>
            {
                return methodParam.Type.Name == "CancellationToken";
            });
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
        static bool ArgumentIsCancellationTokenFromContext(ArgumentSyntax arg, string contextParamName)
        {
            if (!(arg.Expression is MemberAccessExpressionSyntax memberAccess) || !memberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                return false;
            }

            if (!(memberAccess.Expression is IdentifierNameSyntax exprId) || exprId.Identifier.ValueText != contextParamName || memberAccess.Name.Identifier.ValueText != "CancellationToken")
            {
                return false;
            }

            return true;
        }

        // {0} = Variable name for the IMessageHandlerContext parameter
        // {1} = Name of method being invoked
        internal static readonly DiagnosticDescriptor ForwardCancellationTokenFromHandlerDiagnostic = new DiagnosticDescriptor(
            id: "NSB0007",
            title: "Forward CancellationToken From Handler",
            messageFormat: "{0}.CancellationToken should be passed to the {1} method in order to support cooperative cancellation.",
            category: "NServiceBus.Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "In order to support cooperative cancellation, the CancellationToken on the IMessageHandlerContext should be passed to any methods called by the handler so that message processing can be halted cleanly if necessary.");
    }
}
