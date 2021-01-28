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
    public class ForwardCancellationTokenAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            ForwardCancellationTokenDiagnostic
        );

        static AnalysisTarget[] targets;
        static HashSet<string> targetMethodNames;
        static HashSet<string> targetContextNames;

        static ForwardCancellationTokenAnalyzer()
        {
            targets = new[]
            {
                new AnalysisTarget("NServiceBus.IHandleMessages`1", "Handle", "IMessageHandlerContext"),
                new AnalysisTarget("NServiceBus.IAmStartedByMessages`1", "Handle", "IMessageHandlerContext"),
                new AnalysisTarget("NServiceBus.IHandleTimeouts`1", "Timeout", "IMessageHandlerContext"),
                new AnalysisTarget("NServiceBus.Sagas.IHandleSagaNotFound", "Handle", "IMessageProcessingContext"),
                new AnalysisTarget("NServiceBus.Pipeline.Behavior`1", "Invoke", null), // Context based on generic type argument
                new AnalysisTarget("NServiceBus.Pipeline.IBehavior`2", "Invoke", null), // Context based on generic type argument
            };

            targetMethodNames = new HashSet<string>(targets.Select(target => target.MethodName).Distinct());
            targetContextNames = new HashSet<string>(targets.Select(target => target.ContextTypeName).Where(name => name != null).Distinct());
        }

        class AnalysisTarget
        {
            public string TypeName { get; }
            public string MethodName { get; }
            public string ContextTypeName { get; }

            public AnalysisTarget(string typeName, string methodName, string contextTypeName)
            {
                TypeName = typeName;
                MethodName = methodName;
                ContextTypeName = contextTypeName;
            }
        }

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
            public ImmutableArray<INamedTypeSymbol> AnalysisTypes { get; }

            public bool IsValid => CancellationToken != null && IPipelineContext != null;

            public KnownTypes(Compilation compilation)
            {
                CancellationToken = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
                IPipelineContext = compilation.GetTypeByMetadataName("NServiceBus.IPipelineContext");
                AnalysisTypes = targets.Select(target =>
                {
                    var type = compilation.GetTypeByMetadataName(target.TypeName);
                    // Translate IBehavior<TFromContext, TToContext> to IBehavior<,>
                    return type.IsGenericType ? type.ConstructUnboundGenericType() : type;
                }).ToImmutableArray();
            }
        }

        static void AnalyzeSyntax(SyntaxNodeAnalysisContext context, KnownTypes knownTypes)
        {
            if (!(context.Node is InvocationExpressionSyntax invocation))
            {
                return;
            }

            // Get the parent method and make sure it's got one of the names we care about (i.e. Handle, Invoke)
            var parentMethodDeclaration = GetParent<MethodDeclarationSyntax>(invocation);
            if (parentMethodDeclaration == null || !targetMethodNames.Contains(parentMethodDeclaration.Identifier.ValueText))
            {
                return;
            }

            // Make sure the parent (i.e. Handle) method has a parameter that is a known context, learn the type and variable name
            if (!ParametersContainsAValidContext(parentMethodDeclaration, out var contextParamType, out var contextParamName))
            {
                return;
            }

            // Get the owner class declaration and make sure it includes a target type (e.g. IHandle...) in its ancestry
            var classDeclaration = GetParent<ClassDeclarationSyntax>(parentMethodDeclaration);
            if (!ClassInheritsATargetType(context, classDeclaration, knownTypes))
            {
                return;
            }

            // Check cancellation before getting expensive symbol info in ArgumentIsACancellationToken
            if (context.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            var invocationArgs = invocation.ArgumentList.ChildNodes().OfType<ArgumentSyntax>().ToArray();
            if (invocationArgs.Any(arg => ArgumentIsACancellationToken(context, arg, contextParamName, knownTypes)))
            {
                return;
            }

            // Check cancellation again before getting method symbol
            if (context.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (!(context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is IMethodSymbol methodSymbol))
            {
                return;
            }

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

        static bool ParametersContainsAValidContext(MethodDeclarationSyntax parentMethodDeclaration, out string contextParamType, out string contextParamName)
        {
            contextParamType = null;
            contextParamName = null;
            string[] genericTypeArgumentNames = null;

            var parameters = parentMethodDeclaration.ParameterList.ChildNodes().OfType<ParameterSyntax>().ToArray();

            foreach (var parameter in parameters)
            {
                if (parameter.Type is IdentifierNameSyntax contextTypeIdentifier)
                {
                    contextParamType = contextTypeIdentifier.Identifier.ValueText;
                    contextParamName = parameter.Identifier.ValueText;
                    if (targetContextNames.Contains(contextParamType))
                    {
                        return true;
                    }

                    if (contextParamType.EndsWith("Context"))
                    {
                        if (genericTypeArgumentNames == null)
                        {
                            genericTypeArgumentNames = parentMethodDeclaration.Ancestors()
                                .OfType<ClassDeclarationSyntax>()
                                .FirstOrDefault()
                                ?.BaseList
                                ?.DescendantNodes()
                                .OfType<TypeArgumentListSyntax>()
                                .SelectMany(list => list.DescendantNodes().OfType<IdentifierNameSyntax>())
                                .Select(genericTypeNameSyntax => genericTypeNameSyntax.Identifier.ValueText)
                                .ToArray();
                        }

                        if (genericTypeArgumentNames != null && genericTypeArgumentNames.Contains(contextParamType))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        static bool ClassInheritsATargetType(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax classDeclaration, KnownTypes knownTypes)
        {
            if (classDeclaration == null || classDeclaration.BaseList == null)
            {
                return false;
            }

            return classDeclaration.BaseList.DescendantNodes().OfType<SimpleNameSyntax>()
                .Any(baseTypeSyntax =>
                {
                    var baseTypeSymbolInfo = context.SemanticModel.GetSymbolInfo(baseTypeSyntax, context.CancellationToken);
                    if (baseTypeSymbolInfo.Symbol is INamedTypeSymbol baseType)
                    {
                        var nonGenericType = baseType.IsGenericType ? baseType.ConstructUnboundGenericType() : baseType;
                        return knownTypes.AnalysisTypes.Any(analysisType => analysisType.Equals(nonGenericType));
                    }
                    return false;
                });
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

            var diagnostic = Diagnostic.Create(ForwardCancellationTokenDiagnostic, invocation.GetLocation(), properties, contextParamName, methodSymbol.Name);
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
        internal static readonly DiagnosticDescriptor ForwardCancellationTokenDiagnostic = new DiagnosticDescriptor(
            id: "NSB0002",
            title: "Forward `context.CancellationToken` to methods",
            messageFormat: "Forward `{0}.CancellationToken` to the `{1}` method.",
            category: "NServiceBus.Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Forward the `CancellationToken` from the context parameter to methods that take one to ensure the operation cancellation notifications are properly propagated. This ensures that message processing can be halted cleanly if necessary. Consider elevating the severity of \"CA2016: Forward the CancellationToken parameter to methods that take one\" to `warning` to ensure the token is passed correctly to other methods as well.");
    }
}
