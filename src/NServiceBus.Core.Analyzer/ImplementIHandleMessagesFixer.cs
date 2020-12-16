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

        protected override string GetInsertCode(string messageType)
        {
            return @"
public async Task Handle(" + messageType + @" message, IMessageHandlerContext context)
{
}";
        }
    }

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ImplementIHandleMessagesFixer))]
    [Shared]
    public class ImplementIHandleMessagesWithCancellationFixer : AbstractImplementIHandleMessagesFixer
    {
        protected override string Title => "Implement IHandleMessages<T> with Cancellation";

        protected override string GetInsertCode(string messageType)
        {
            return @"
public async Task Handle(" + messageType + @" message, IMessageHandlerContext context, CancellationToken cancellationToken)
{
}";
        }
    }

    public abstract class AbstractImplementIHandleMessagesFixer : CodeFixProvider
    {
        protected abstract string Title { get; }

        protected abstract string GetInsertCode(string messageType);

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
            var insertCode = GetInsertCode(messageType);

            var newSyntaxTree = CSharpSyntaxTree.ParseText(insertCode, cancellationToken: cancellationToken);
            var newMethodDeclaration = (await newSyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false))
                .ChildNodes().OfType<MethodDeclarationSyntax>().First()
                .WithAdditionalAnnotations(Formatter.Annotation);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var newClassDeclaration = classDeclaration.AddMembers(newMethodDeclaration);

            var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);

            var newDocument = document.WithSyntaxRoot(newRoot);

            return newDocument;
        }
    }
}
