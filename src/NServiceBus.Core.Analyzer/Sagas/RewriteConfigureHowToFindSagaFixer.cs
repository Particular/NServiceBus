namespace NServiceBus.Core.Analyzer
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Formatting;
    using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

    [Shared]
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RewriteConfigureHowToFindSagaFixer))]
    public class RewriteConfigureHowToFindSagaFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(
                SagaDiagnostics.SagaMappingExpressionCanBeSimplifiedId,
                SagaDiagnostics.MessageStartsSagaButNoMappingId,
                SagaDiagnostics.SagaMappingExpressionCanBeRewrittenId);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        static readonly string NewLine = System.Environment.NewLine;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            // Since all the diagnostics have the same span
            // https://github.com/dotnet/roslyn-api-docs/blob/live/dotnet/xml/Microsoft.CodeAnalysis.CodeFixes/CodeFixContext.xml#L187
            // and the fixer only fixes one type of diagnostic,
            // we can assume there is only one diagnostic.
            // If there are duplicates then the analyzer is broken and its tests should catch that.
            var diagnostic = context.Diagnostics.First();

            if (!FixableDiagnosticIds.Contains(diagnostic.Id))
            {
                return;
            }

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var title = diagnostic.Properties["_FixerTitle"];
            var correlationId = diagnostic.Properties["_CorrelationId"];
            var mapperParamName = diagnostic.Properties["_MapperParamName"];
            var mappingCount = int.Parse(diagnostic.Properties["_MappingCount"]);
            var configureHowToFindLocationStart = int.Parse(diagnostic.Properties["_ConfigureHowToFindLocation"]);

            var configureMethodToken = root.FindToken(configureHowToFindLocationStart);
            var method = configureMethodToken.Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

            var mappings = Enumerable.Range(0, mappingCount)
                .Select(i =>
                {
                    var by2Index = i * 2;
                    var msgTypeLocation = diagnostic.AdditionalLocations[by2Index];
                    var mappingExpressionLocation = diagnostic.AdditionalLocations[by2Index + 1];
                    var isHeader = bool.Parse(diagnostic.Properties[i.ToString()]);

                    var messageType = root.FindToken(msgTypeLocation.SourceSpan.Start)
                        .Parent.AncestorsAndSelf().OfType<IdentifierNameSyntax>().First();
                    var mappingExpression = root.FindToken(mappingExpressionLocation.SourceSpan.Start)
                        .Parent.AncestorsAndSelf().OfType<ArgumentSyntax>().First();

                    return new SagaMessageMapping(messageType, null, isHeader, mappingExpression, null);
                });

            if (diagnostic.Properties.TryGetValue("_NewMappingForLocation", out var newMappingForLocation))
            {
                var baseTypeSyntax = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.AncestorsAndSelf().OfType<BaseTypeSyntax>().First();
                var messageType = (baseTypeSyntax.Type as GenericNameSyntax).TypeArgumentList.Arguments[0];
                var newMapping = SagaMessageMapping.CreateNewMapping(messageType, newMappingForLocation, correlationId);
                mappings = mappings.Concat(new[] { newMapping });
            }

            // Register a code action that will invoke the fix
            var action = CodeAction.Create(
                title,
                cancellationToken => RewriteConfigureHowToFindSagaMethod(context.Document, method, correlationId, mapperParamName, mappings.ToImmutableArray(), cancellationToken),
                EquivalenceKey);

            context.RegisterCodeFix(action, diagnostic);
        }

        static async Task<Document> RewriteConfigureHowToFindSagaMethod(
            Document document,
            MethodDeclarationSyntax method,
            string correlationId,
            string mapperParamName,
            ImmutableArray<SagaMessageMapping> mappings,
            CancellationToken cancellationToken)
        {
            var leadingTrivia = method.GetLeadingTrivia();
            var triviaDirectlyBefore = leadingTrivia.Reverse().FirstOrDefault();
            var baseIndentText = string.Empty;
            if (triviaDirectlyBefore.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                baseIndentText = triviaDirectlyBefore.ToString();
            }

            var indentText = await GetIndentString(document, cancellationToken).ConfigureAwait(false);

            SyntaxTrivia Indent(int levels = 0)
            {
                // TODO: Probably a way to make this perform better
                var indent = baseIndentText;
                for (var i = 0; i < levels; i++)
                {
                    indent += indentText;
                }
                return Whitespace(indent);
            }

            var mapSagaInvocation = InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(mapperParamName).WithLeadingTrivia(Indent(1)),
                    IdentifierName("MapSaga")))
                .WithArgumentList(CreateCorrelationIdMapping(correlationId));

            for (int i = 0; i < mappings.Length; i++)
            {
                var mapping = mappings[i];

                mapSagaInvocation = InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        mapSagaInvocation.WithTrailingTrivia(EndOfLine(NewLine)),
                        Token(SyntaxKind.DotToken).WithLeadingTrivia(Indent(2)),
                        CreateGenericMappingMethod(mapping.IsHeader ? "ToMessageHeader" : "ToMessage", mapping.MessageTypeSyntax.ToFullString())
                    )
                )
                .WithArgumentList(
                    ArgumentList(
                        SingletonSeparatedList(mapping.MessageMappingExpression)
                    )
                );
            }

            var block = Block(
                Token(SyntaxKind.OpenBraceToken).WithLeadingTrivia(Indent()).WithTrailingTrivia(EndOfLine(NewLine)),
                SingletonList<StatementSyntax>(
                    ExpressionStatement(mapSagaInvocation, Token(SyntaxKind.SemicolonToken).WithTrailingTrivia(EndOfLine(NewLine)))
                ),
                Token(SyntaxKind.CloseBraceToken).WithLeadingTrivia(Indent())
            );

            // For debugging only
            //System.Console.WriteLine("====== Block Full String =======");
            //System.Console.WriteLine(block.ToFullString());
            //System.Console.WriteLine("================================");

            var newMethod = MethodDeclaration(
                    PredefinedType(Token(SyntaxKind.VoidKeyword)),
                    Identifier("ConfigureHowToFindSaga"))
                .WithModifiers(
                    TokenList(
                        new[]{
                            Token(SyntaxKind.ProtectedKeyword),
                            Token(SyntaxKind.OverrideKeyword)}))
                .WithParameterList(method.ParameterList.NormalizeWhitespace())
                .NormalizeWhitespace().WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(EndOfLine(NewLine))
                .WithBody(block)
                .WithExpressionBody(null)
                .WithTrailingTrivia(method.GetTrailingTrivia());

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(method, newMethod);
            var newDocument = document.WithSyntaxRoot(newRoot);

            return newDocument;
        }

        static GenericNameSyntax CreateGenericMappingMethod(string methodName, string messageType)
        {
            return GenericName(methodName)
                .WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList<TypeSyntax>(IdentifierName(messageType))));
        }

        static ArgumentListSyntax CreateCorrelationIdMapping(string correlationId)
        {
            return ArgumentList(
                SingletonSeparatedList(
                    Argument(
                        SimpleLambdaExpression(
                            Parameter(Identifier("saga")),
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("saga"),
                                IdentifierName(correlationId))))));
        }

        static async Task<string> GetIndentString(Document document, CancellationToken cancellationToken)
        {
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var useTabs = options.GetOption(FormattingOptions.UseTabs);
            var indentSize = options.GetOption(FormattingOptions.IndentationSize);

            if (useTabs && indentSize == 1)
            {
                return "\t";
            }
            else if (!useTabs && indentSize == 4)
            {
                return "    ";
            }
            else
            {
                var indentBuilder = new StringBuilder();
                var indentChar = useTabs ? '\t' : ' ';
                for (var i = 0; i < indentSize; i++)
                {
                    indentBuilder.Append(indentChar);
                }
                return indentBuilder.ToString();
            }
        }

        // Value really doesn't matter but is required by RS1010 analyzer. Can not be the title because
        // the title is dynamic based on the context name and target method name.
        static readonly string EquivalenceKey = typeof(RewriteConfigureHowToFindSagaFixer).FullName;

    }
}
