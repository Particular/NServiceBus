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
    public class ImplementIHandleMessagesFixer : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            MustImplementIHandleMessagesAnalyzer.MustImplementIHandleMessagesDiagnostic.Id,
            MustImplementIHandleMessagesAnalyzer.MustImplementIAmStartedByMessagesDiagnostic.Id,
            MustImplementIHandleMessagesAnalyzer.MustImplementIHandleTimeoutsDiagnostic.Id
        );

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var methodName = diagnostic.Properties["FixerMethodName"];
            var messageType = diagnostic.Properties["MessageType"];
            var sourceLocation = diagnostic.Location;
            var diagnosticSpan = sourceLocation.SourceSpan;
            var interfaceName = sourceLocation.SourceTree.ToString().Substring(diagnosticSpan.Start, diagnosticSpan.Length);

            var interfaceNameToken = root.FindToken(diagnosticSpan.Start);
            var classDeclaration = interfaceNameToken.Parent.Ancestors().OfType<ClassDeclarationSyntax>().First();

            var title = "Implement " + interfaceName;

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
              CodeAction.Create(title, c =>
              ImplementIHandleMessages(context.Document, classDeclaration, methodName, messageType, c), equivalenceKey: title), diagnostic);
        }

        private async Task<Document> ImplementIHandleMessages(Document document, ClassDeclarationSyntax classDeclaration, string methodName, string messageType, CancellationToken cancellationToken)
        {
            var parameterList = new SeparatedSyntaxList<ParameterSyntax>()
                .Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier("message")).WithType(SyntaxFactory.ParseTypeName(messageType)))
                .Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier("context")).WithType(SyntaxFactory.ParseTypeName("IMessageHandlerContext")))
                .Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier("cancellationToken")).WithType(SyntaxFactory.ParseTypeName("CancellationToken")));

            var newMethodDeclaration = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("Task"), methodName)
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
