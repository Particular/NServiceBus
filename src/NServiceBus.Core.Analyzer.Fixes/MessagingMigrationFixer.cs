#nullable enable

namespace NServiceBus.Core.Analyzer.Fixes;

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MessagingMigrationFixer))]
public sealed class MessagingMigrationFixer : CodeFixProvider
{
    const string MessageTypeProperty = "MessageType";
    const string EquivalenceKey = "UseStronglyTypedMessageOverload";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [DiagnosticIds.UseGenericMessageType];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

        foreach (var diagnostic in context.Diagnostics)
        {
            if (!diagnostic.Properties.TryGetValue(MessageTypeProperty, out var messageType) ||
                string.IsNullOrWhiteSpace(messageType))
            {
                continue;
            }

            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

            if (node is ExpressionSyntax methodReference && CanAddTypeArgument(methodReference))
            {
                if (!CanOfferFix(semanticModel, methodReference))
                {
                    continue;
                }

                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Use the strongly typed message overload",
                        cancellationToken => AddTypeArgumentToMethodReference(
                            context.Document,
                            root,
                            methodReference,
                            messageType!,
                            cancellationToken),
                        EquivalenceKey),
                    diagnostic);
                continue;
            }

            if (node.FirstAncestorOrSelf<InvocationExpressionSyntax>() is not { } invocation ||
                !CanAddTypeArgument(invocation.Expression))
            {
                continue;
            }

            if (!CanOfferFix(semanticModel, invocation.Expression))
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Use the strongly typed message overload",
                    cancellationToken => AddTypeArgument(
                        context.Document,
                        root,
                        invocation,
                        messageType!,
                        cancellationToken),
                    EquivalenceKey),
                diagnostic);
        }
    }

    static bool CanAddTypeArgument(ExpressionSyntax expression) => expression switch
    {
        MemberAccessExpressionSyntax { Name: IdentifierNameSyntax } => true,
        MemberBindingExpressionSyntax { Name: IdentifierNameSyntax } => true,
        IdentifierNameSyntax => true,
        _ => false
    };

    // Default interface members are not callable through a concrete receiver.
    static bool CanOfferFix(SemanticModel? semanticModel, ExpressionSyntax expression)
    {
        if (semanticModel is null || expression is not (
            MemberAccessExpressionSyntax or MemberBindingExpressionSyntax or IdentifierNameSyntax))
        {
            return false;
        }

        var methodName = expression switch
        {
            MemberAccessExpressionSyntax { Name: IdentifierNameSyntax name } => name.Identifier.ValueText,
            MemberBindingExpressionSyntax { Name: IdentifierNameSyntax name } => name.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            _ => null
        };
        if (methodName is null)
        {
            return false;
        }

        var receiverType = GetReceiverType(semanticModel, expression);
        if (receiverType is null || receiverType.TypeKind == TypeKind.Error)
        {
            return false;
        }

        if (receiverType.TypeKind == TypeKind.Interface)
        {
            return true;
        }

        var within = semanticModel.GetEnclosingSymbol(expression.SpanStart)?.ContainingType;
        for (var type = receiverType; type is not null; type = type.BaseType)
        {
            foreach (var member in type.GetMembers(methodName).OfType<IMethodSymbol>())
            {
                if (!member.IsGenericMethod || member.TypeParameters.Length == 0)
                {
                    continue;
                }

                // Exclude creator overloads such as Send<T>(Action<T>, ...).
                var messageTypeParameter = member.TypeParameters[0];
                if (!member.Parameters.Any(parameter =>
                        SymbolEqualityComparer.Default.Equals(parameter.Type, messageTypeParameter)))
                {
                    continue;
                }

                if (within is null
                    ? member.DeclaredAccessibility == Accessibility.Public
                    : semanticModel.Compilation.IsSymbolAccessibleWithin(member, within))
                {
                    return true;
                }
            }
        }

        return false;
    }

    static ITypeSymbol? GetReceiverType(SemanticModel semanticModel, ExpressionSyntax expression)
    {
        var receiverSyntax = expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Expression,
            MemberBindingExpressionSyntax memberBinding when
                memberBinding.Parent is ConditionalAccessExpressionSyntax conditional => conditional.Expression,
            _ => null
        };

        if (receiverSyntax is not null)
        {
            return semanticModel.GetTypeInfo(receiverSyntax).Type;
        }

        return semanticModel.GetEnclosingSymbol(expression.SpanStart)?.ContainingType;
    }

    static Task<Document> AddTypeArgument(
        Document document,
        SyntaxNode root,
        InvocationExpressionSyntax invocation,
        string messageType,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var updatedInvocation = invocation.WithExpression(AddTypeArgumentToExpression(invocation.Expression, messageType))
            .WithAdditionalAnnotations(Formatter.Annotation);
        return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(invocation, updatedInvocation)));
    }

    static Task<Document> AddTypeArgumentToMethodReference(
        Document document,
        SyntaxNode root,
        ExpressionSyntax methodReference,
        string messageType,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var updatedMethodReference = AddTypeArgumentToExpression(methodReference, messageType)
            .WithAdditionalAnnotations(Formatter.Annotation);
        return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(methodReference, updatedMethodReference)));
    }

    static ExpressionSyntax AddTypeArgumentToExpression(ExpressionSyntax expression, string messageType)
    {
        var typeArgument = SyntaxFactory.ParseTypeName(messageType)
            .WithAdditionalAnnotations(Simplifier.Annotation);
        var typeArguments = SyntaxFactory.TypeArgumentList(
            SyntaxFactory.SingletonSeparatedList(typeArgument));

        return expression switch
        {
            MemberAccessExpressionSyntax { Name: IdentifierNameSyntax name } memberAccess =>
                memberAccess.WithName(
                    SyntaxFactory.GenericName(name.Identifier, typeArguments)
                        .WithTriviaFrom(name)),
            MemberBindingExpressionSyntax { Name: IdentifierNameSyntax name } memberBinding =>
                memberBinding.WithName(
                    SyntaxFactory.GenericName(name.Identifier, typeArguments)
                        .WithTriviaFrom(name)),
            IdentifierNameSyntax name =>
                SyntaxFactory.GenericName(name.Identifier, typeArguments)
                    .WithTriviaFrom(name),
            _ => expression
        };
    }
}
