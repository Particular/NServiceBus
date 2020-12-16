namespace NServiceBus.Core.Analyzer
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Formatting;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ImplementIHandleMessagesFixer))]
    [Shared]
    public class ImplementIHandleMessagesFixer : AbstractImplementIHandleMessagesFixer
    {
        protected override string Title => "Implement IHandleMessages<T>";

        protected override bool UseCancellation => false;
    }

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ImplementIHandleMessagesFixer))]
    [Shared]
    public class ImplementIHandleMessagesWithCancellationFixer : AbstractImplementIHandleMessagesFixer
    {
        protected override string Title => "Implement IHandleMessages<T> with Cancellation";

        protected override bool UseCancellation => true;
    }

    public abstract class AbstractImplementIHandleMessagesFixer : CodeFixProvider
    {
        protected abstract string Title { get; }

        protected abstract bool UseCancellation { get; }

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MustImplementIHandleMessagesAnalyzer.MustImplementDiagnostic.Id);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var messageType = diagnostic.Properties["MessageType"];
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var interfaceNameToken = root.FindToken(diagnosticSpan.Start);
            var classDeclaration = interfaceNameToken.Parent.Ancestors().OfType<ClassDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
              CodeAction.Create(Title, c =>
              ImplementIHandleMessages(context.Document, classDeclaration, messageType, c), equivalenceKey: Title), diagnostic);
        }

        private async Task<Document> ImplementIHandleMessages(Document document, ClassDeclarationSyntax classDeclaration, string messageType, CancellationToken cancellationToken)
        {
            var parameterList = new SeparatedSyntaxList<ParameterSyntax>()
                .Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier("message")).WithType(SyntaxFactory.ParseTypeName(messageType)))
                .Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier("context")).WithType(SyntaxFactory.ParseTypeName("IMessageHandlerContext")));

            if (UseCancellation)
            {
                parameterList = parameterList.Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken")).WithType(SyntaxFactory.ParseTypeName("CancellationToken")));
            }

            var newMethodDeclaration = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("Task"), "Handle")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword))
                .WithParameterList(SyntaxFactory.ParameterList(parameterList))
                .WithBody(SyntaxFactory.Block())
                .WithAdditionalAnnotations(Formatter.Annotation);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var newClassDeclaration = classDeclaration.AddMembers(newMethodDeclaration);

            var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);

            var newDocument = document.WithSyntaxRoot(newRoot);

            return newDocument;
        }
    }
}
