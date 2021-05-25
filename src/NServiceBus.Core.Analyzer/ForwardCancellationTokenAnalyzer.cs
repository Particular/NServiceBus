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

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(diagnosticDescriptor);

        public override void Initialize(AnalysisContext context) =>
            context.WithDefaultSettings().RegisterCompilationStartAction(Analyze);

        static void Analyze(CompilationStartAnalysisContext startContext)
        {
            // "Feature 'not pattern' is not available in C# 7.3. Please use language version 9.0 or greater." (╯°□°）╯︵ ┻━┻
            if (!(startContext.Compilation.GetTypeByMetadataName("NServiceBus.ICancellableContext") is INamedTypeSymbol cancellableContextInterface))
            {
                return;
            }

            if (!(startContext.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken") is INamedTypeSymbol cancellationTokenType))
            {
                return;
            }

            var genericTaskType = startContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            var genericValueTaskType = startContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

            startContext.RegisterSyntaxNodeAction(
                context => Analyze(context, cancellableContextInterface, cancellationTokenType, genericTaskType, genericValueTaskType),
                SyntaxKind.MethodDeclaration,
                SyntaxKind.AnonymousMethodExpression,
                SyntaxKind.SimpleLambdaExpression,
                SyntaxKind.ParenthesizedLambdaExpression);
        }

        static void Analyze(
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol cancellableContextInterface,
            INamedTypeSymbol cancellationTokenType,
            INamedTypeSymbol genericTaskType,
            INamedTypeSymbol genericValueTaskType)
        {
            IMethodSymbol method;
            SyntaxNode body;

            if (context.Node is BaseMethodDeclarationSyntax declaration)
            {
                method = context.SemanticModel.GetDeclaredSymbol(declaration, context.CancellationToken);
                body = (SyntaxNode)declaration.Body ?? declaration.ExpressionBody;
            }
            else if (context.Node is AnonymousFunctionExpressionSyntax expression)
            {
                method = context.SemanticModel.GetSymbolInfo(expression, context.CancellationToken).Symbol as IMethodSymbol;
                body = expression.Body;
            }
            else
            {
                return;
            }

            if (method == null || body == null)
            {
                return;
            }

            // if method has no cancellable context param
            if (!(method.Parameters.FirstOrDefault(param => param.Type.Implements(cancellableContextInterface)) is IParameterSymbol cancellableContextParam))
            {
                return;
            }

            foreach (var call in body
                .DescendantNodesAndSelf(descendant =>
                    !(descendant is BaseMethodDeclarationSyntax) &&
                    !(descendant is AnonymousFunctionExpressionSyntax))
                .OfType<InvocationExpressionSyntax>())
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                Analyze(call, cancellableContextParam, cancellableContextInterface, cancellationTokenType, genericTaskType, genericValueTaskType, context);
            }
        }

        static void Analyze(
            InvocationExpressionSyntax call,
            IParameterSymbol callerCancellableContextParam,
            INamedTypeSymbol cancellableContextInterface,
            INamedTypeSymbol cancellationTokenType,
            INamedTypeSymbol genericTaskType,
            INamedTypeSymbol genericValueTaskType,
            SyntaxNodeAnalysisContext context)
        {
            // if the call at least looks like it has a cancellation token arg
            if (call.ArgumentList.Arguments.Any(arg =>
                CouldBeCancellationToken(arg.Expression) &&
                (LooksLikeCancellationToken(arg.Expression, callerCancellableContextParam.Name) ||
                    IsCancellationToken(arg.Expression, context, cancellationTokenType))))
            {
                return;
            }

            if (!(context.SemanticModel.GetSymbolInfo(call, context.CancellationToken).Symbol is IMethodSymbol calledMethod))
            {
                return;
            }

            // short-circuit our extension methods on ICancellableContext such as Send(), Publish()
            if (calledMethod.ContainingNamespace?.Name == "NServiceBus" && calledMethod.Extends(cancellableContextInterface))
            {
                return;
            }

            if (!(GetRecommendedMethod(calledMethod, cancellationTokenType, genericTaskType, genericValueTaskType, call, context, out var requiredArgName) is IMethodSymbol recommendedMethod))
            {
                return;
            }

            var properties = new Dictionary<string, string>
            {
                { "CallerCancellableContextParamName", callerCancellableContextParam.Name },
                { "CalledMethodName", calledMethod.Name },
                { "RequiredArgName", requiredArgName },
            }.ToImmutableDictionary();

            var diagnostic = Diagnostic.Create(diagnosticDescriptor, call.GetLocation(), properties, callerCancellableContextParam.Name, calledMethod);

            context.ReportDiagnostic(diagnostic);
        }

        // may return false positives
        static bool CouldBeCancellationToken(ExpressionSyntax expression)
        {
            switch (expression)
            {
                // 3, 'x', default, etc....
                case LiteralExpressionSyntax literal:
                    // only the default literal can be a cancellation token
                    return literal.Kind() == SyntaxKind.DefaultLiteralExpression;
                default:
                    return true;
            }
        }

        static bool LooksLikeCancellationToken(ExpressionSyntax expression, string callerCancellableContextParam)
        {
            if (expression is MemberAccessExpressionSyntax memberAccess)
            {
                if (memberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression) &&
                    memberAccess.Expression is SimpleNameSyntax @ref)
                {
                    var refName = @ref.Identifier.ValueText;
                    var memberName = memberAccess.Name.Identifier.ValueText;

                    // Is context.CancellationToken
                    if (refName == callerCancellableContextParam && memberName == "CancellationToken")
                    {
                        return true;
                    }

                    if (refName == "CancellationToken" && memberName == "None")
                    {
                        return true;
                    }
                }
            }

            if (expression is ObjectCreationExpressionSyntax objectCreation &&
                objectCreation.Type is IdentifierNameSyntax objectCreationTypeName &&
                objectCreationTypeName.Identifier.ValueText == "CancellationToken")
            {
                return true;
            }

            return false;
        }

        static bool IsCancellationToken(ExpressionSyntax expressionSyntax, SyntaxNodeAnalysisContext context, INamedTypeSymbol cancellationTokenType)
        {
            var expressionSymbol = context.SemanticModel.GetSymbolInfo(expressionSyntax, context.CancellationToken).Symbol;
            return expressionSymbol.GetTypeSymbolOrDefault()?.Equals(cancellationTokenType) ?? false;
        }

        static IMethodSymbol GetRecommendedMethod(
            IMethodSymbol calledMethod,
            INamedTypeSymbol cancellationTokenType,
            INamedTypeSymbol genericTaskType,
            INamedTypeSymbol genericValueTaskType,
            InvocationExpressionSyntax call,
            SyntaxNodeAnalysisContext context,
            out string requiredArgName)
        {
            requiredArgName = null;

            var extraParam = calledMethod.Parameters.FirstOrDefault(param => param.Type.Equals(cancellationTokenType));

            if (extraParam != null)
            {
                requiredArgName = GetRequiredArgName(calledMethod, extraParam, call.ArgumentList.Arguments);

                // the called method has an optional cancellation token param
                // but no argument is being passed
                return calledMethod;
            }

            var calledType = GetCalledType(call, calledMethod, context);

            // Walk the methods of the called type, and its ancestors if applicable, and
            // look for an overload which only requires a cancellation token more.
            // We do not search for extension methods because CA2016 doesn't.
            var types = calledMethod.IsStatic ? new[] { calledType } : calledType.BaseTypesAndSelf().Where(type => !type.IsSystemObjectType());

            var overloads = types.SelectMany(type => type.GetMembers(calledMethod.Name).OfType<IMethodSymbol>());

            var overloadsWithASingleCancellationTokenLast = overloads
                .Where(overload =>
                    overload.Parameters.Count(param => param.Type.Equals(cancellationTokenType)) == 1 &&
                    overload.Parameters.Last().Type.Equals(cancellationTokenType))
                .Select(overload => (Overload: overload, CancellationTokenParam: overload.Parameters.Last()));

            var candidates = overloadsWithASingleCancellationTokenLast
                .Where(item => HasSameParametersPlusCancellationToken(genericTaskType, genericValueTaskType, calledMethod, item.Overload));

            var (recommendedMethod, cancellationTokenParam) = candidates.FirstOrDefault();

            if (recommendedMethod == null)
            {
                return null;
            }

            requiredArgName = GetRequiredArgName(recommendedMethod, cancellationTokenParam, call.ArgumentList.Arguments);

            return recommendedMethod;
        }

        // if adding a cancellation token to the args will not put it in the right place
        static string GetRequiredArgName(IMethodSymbol recommendedMethod, IParameterSymbol extraParam, SeparatedSyntaxList<ArgumentSyntax> args) =>
            recommendedMethod.Parameters[args.Count] != extraParam ? extraParam.Name : null;

        static INamedTypeSymbol GetCalledType(InvocationExpressionSyntax call, IMethodSymbol calledMethod, SyntaxNodeAnalysisContext context)
        {
            if (call.Expression is IdentifierNameSyntax)
            {
                return calledMethod.ContainingType;
            }

            if (!(call.Expression is MemberAccessExpressionSyntax memberAccess))
            {
                return calledMethod.ContainingType;
            }

            if (!(memberAccess.Expression is IdentifierNameSyntax refSyntax))
            {
                return calledMethod.ContainingType;
            }

            var @ref = context.SemanticModel.GetSymbolInfo(refSyntax, context.CancellationToken).Symbol;

            if (@ref.GetTypeSymbolOrDefault() is INamedTypeSymbol type)
            {
                return type;
            }

            return calledMethod.ContainingType;
        }

        // largely copied from https://github.com/dotnet/roslyn-analyzers/blob/8236e8bdf092bd9ae21cf42d12b8c480459b5e36/src/NetAnalyzers/Core/Microsoft.NetCore.Analyzers/Runtime/ForwardCancellationTokenToInvocations.Analyzer.cs#L349
        // Checks if the parameters of the two passed methods only differ in a ct.
        static bool HasSameParametersPlusCancellationToken(
            INamedTypeSymbol genericTask,
            INamedTypeSymbol genericValueTask,
            IMethodSymbol originalMethod,
            IMethodSymbol methodToCompare)
        {
            // Avoid comparing to itself, or when there are no parameters, or when the last parameter is not a ct
            if (originalMethod.Equals(methodToCompare))
            {
                return false;
            }

            IMethodSymbol originalMethodWithAllParameters = (originalMethod.ReducedFrom ?? originalMethod).OriginalDefinition;
            IMethodSymbol methodToCompareWithAllParameters = (methodToCompare.ReducedFrom ?? methodToCompare).OriginalDefinition;

            // Ensure parameters only differ by one - the ct
            if (originalMethodWithAllParameters.Parameters.Length != methodToCompareWithAllParameters.Parameters.Length - 1)
            {
                return false;
            }

            // Now compare the types of all parameters before the ct
            // The largest i is the number of parameters in the method that has fewer parameters
            for (int i = 0; i < originalMethodWithAllParameters.Parameters.Length; i++)
            {
                IParameterSymbol originalParameter = originalMethodWithAllParameters.Parameters[i];
                IParameterSymbol comparedParameter = methodToCompareWithAllParameters.Parameters[i];
                if (!originalParameter.Type.Equals(comparedParameter.Type))
                {
                    return false;
                }
            }

            // Overload is  valid if its return type is implicitly convertable
            var toCompareReturnType = methodToCompareWithAllParameters.ReturnType;
            var originalReturnType = originalMethodWithAllParameters.ReturnType;
            if (!toCompareReturnType.IsAssignableTo(originalReturnType))
            {
                // Generic Task-like types are special since awaiting them essentially erases the task-like type.
                // If both types are Task-like we will warn if their generic arguments are convertable to each other.
                if (IsTaskLikeType(originalReturnType) && IsTaskLikeType(toCompareReturnType) &&
                    originalReturnType is INamedTypeSymbol originalNamedType &&
                    toCompareReturnType is INamedTypeSymbol toCompareNamedType &&
                    TypeArgumentsAreConvertable(originalNamedType, toCompareNamedType))
                {
                    return true;
                }

                return false;
            }

            return true;

            bool IsTaskLikeType(ITypeSymbol typeSymbol)
            {
                if (genericTask != null &&
                    typeSymbol.OriginalDefinition.Equals(genericTask))
                {
                    return true;
                }

                if (genericValueTask != null &&
                    typeSymbol.OriginalDefinition.Equals(genericValueTask))
                {
                    return true;
                }

                return false;
            }

            bool TypeArgumentsAreConvertable(INamedTypeSymbol left, INamedTypeSymbol right)
            {
                if (left.Arity != 1 ||
                    right.Arity != 1 ||
                    left.Arity != right.Arity)
                {
                    return false;
                }

                var leftTypeArgument = left.TypeArguments[0];
                var rightTypeArgument = right.TypeArguments[0];
                if (!leftTypeArgument.IsAssignableTo(rightTypeArgument))
                {
                    return false;
                }

                return true;
            }
        }

        static readonly DiagnosticDescriptor diagnosticDescriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Forward the 'CancellationToken' property of the context parameter to methods",
            messageFormat: "Forward '{0}.CancellationToken' to the '{1}' method or pass in 'CancellationToken.None' explicitly to indicate intentionally not propagating the token",
            category: "NServiceBus.Code",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Forward the 'CancellationToken' property of the context parameter to methods to ensure the operation cancellation notifications are properly propagated, or pass in 'CancellationToken.None' explicitly to indicate intentionally not propagating the token. Forwarding the token allows cancellation of message processing when necessary. Also consider elevating the severity of \"CA2016: Forward the 'CancellationToken parameter' to methods\" to 'warning' to ensure 'CancellationToken' parameters are forwarded appropriately.");
    }
}
