namespace NServiceBus.Core.Analyzer.Fixes
{
    using System.Collections.Generic;
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
    using Microsoft.CodeAnalysis.Editing;
    using Microsoft.CodeAnalysis.Formatting;

    [Shared]
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(HandlerAttributeFixer))]
    public class HandlerAttributeFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            [DiagnosticIds.HandlerAttributeMissing, DiagnosticIds.HandlerAttributeMisplaced, DiagnosticIds.HandlerAttributeOnNonHandler];

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                if (root?.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is not { } node)
                {
                    continue;
                }

                var classDecl = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
                if (classDecl is null)
                {
                    continue;
                }

                switch (diagnostic.Id)
                {
                    case DiagnosticIds.HandlerAttributeMissing:
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Add HandlerAttribute",
                                token => AddHandlerAttribute(context.Document, classDecl, token),
                                EquivalenceKeyAdd),
                            diagnostic);
                        break;
                    case DiagnosticIds.HandlerAttributeMisplaced:
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Move HandlerAttribute to concrete handlers",
                                token => MoveHandlerAttribute(context.Document, classDecl, token),
                                EquivalenceKeyMove),
                            diagnostic);
                        break;
                    case DiagnosticIds.HandlerAttributeOnNonHandler:
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Remove HandlerAttribute",
                                token => RemoveHandlerAttribute(context.Document, classDecl, token),
                                EquivalenceKeyRemove),
                            diagnostic);
                        break;
                    default:
                        break;
                }
            }
        }

        static async Task<Document> AddHandlerAttribute(
            Document document,
            ClassDeclarationSyntax classDeclaration,
            CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var updatedClass = AddHandlerAttributeToClass(classDeclaration);
            editor.ReplaceNode(classDeclaration, updatedClass);

            var changed = editor.GetChangedDocument();
            var root = await changed.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var formattedRoot = Formatter.Format(root, Formatter.Annotation, changed.Project.Solution.Workspace, cancellationToken: cancellationToken);
            return changed.WithSyntaxRoot(formattedRoot);
        }

        static async Task<Document> RemoveHandlerAttribute(
            Document document,
            ClassDeclarationSyntax classDeclaration,
            CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var semanticModel = editor.SemanticModel;

            var compilation = semanticModel.Compilation;
            // TODO cache?
            var iHandleMessages = compilation.GetTypeByMetadataName("NServiceBus.IHandleMessages`1");
            var handlerAttributeSymbol = compilation.GetTypeByMetadataName("NServiceBus.HandlerAttribute");
            if (iHandleMessages is null || handlerAttributeSymbol is null)
            {
                return document;
            }

            var updatedClass = RemoveHandlerAttribute(classDeclaration, semanticModel, handlerAttributeSymbol, cancellationToken);
            editor.ReplaceNode(classDeclaration, updatedClass);

            var changed = editor.GetChangedDocument();
            var root = await changed.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var formattedRoot = Formatter.Format(root, Formatter.Annotation, changed.Project.Solution.Workspace, cancellationToken: cancellationToken);
            return changed.WithSyntaxRoot(formattedRoot);
        }

        static async Task<Solution> MoveHandlerAttribute(
            Document document,
            ClassDeclarationSyntax baseClassDeclaration,
            CancellationToken cancellationToken)
        {
            _ = baseClassDeclaration;
            var solution = document.Project.Solution;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            if (semanticModel is null)
            {
                return solution;
            }

            var compilation = semanticModel.Compilation;
            var iHandleMessages = compilation.GetTypeByMetadataName("NServiceBus.IHandleMessages`1");
            var handlerAttributeSymbol = compilation.GetTypeByMetadataName("NServiceBus.HandlerAttribute");
            if (iHandleMessages is null || handlerAttributeSymbol is null)
            {
                return solution;
            }

            var handlerTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            var baseTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

            foreach (var type in compilation.Assembly.GlobalNamespace.GetAllNamedTypes())
            {
                if (type.DeclaringSyntaxReferences.Length == 0)
                {
                    continue;
                }

                if (type.TypeKind != TypeKind.Class)
                {
                    continue;
                }

                if (!type.ImplementsGenericInterface(iHandleMessages))
                {
                    continue;
                }

                handlerTypes.Add(type);

                if (type.BaseType is { SpecialType: not SpecialType.System_Object } baseType)
                {
                    baseTypes.Add(baseType.OriginalDefinition);
                }
            }

            var leafHandlers = new List<INamedTypeSymbol>();
            var nonLeafHandlersWithAttribute = new List<INamedTypeSymbol>();

            foreach (var type in handlerTypes)
            {
                var isLeaf = !type.IsAbstract && !baseTypes.Contains(type.OriginalDefinition);
                var hasAttribute = type.HasAttribute(handlerAttributeSymbol);

                if (isLeaf)
                {
                    if (!hasAttribute)
                    {
                        leafHandlers.Add(type);
                    }
                }
                else if (hasAttribute)
                {
                    nonLeafHandlersWithAttribute.Add(type);
                }
            }

            var editors = new Dictionary<DocumentId, DocumentEditor>();

            foreach (var handlerType in nonLeafHandlersWithAttribute)
            {
                foreach (var syntaxRef in handlerType.DeclaringSyntaxReferences)
                {
                    var doc = solution.GetDocument(syntaxRef.SyntaxTree);
                    if (doc is null)
                    {
                        continue;
                    }

                    var editor = await GetEditor(doc.Id).ConfigureAwait(false);
                    if (await syntaxRef.GetSyntaxAsync(cancellationToken).ConfigureAwait(false) is ClassDeclarationSyntax classDecl)
                    {
                        var updated = RemoveHandlerAttribute(classDecl, editor.SemanticModel, handlerAttributeSymbol, cancellationToken);
                        editor.ReplaceNode(classDecl, updated);
                    }
                }
            }

            foreach (var leafHandler in leafHandlers)
            {
                var syntaxRef = leafHandler.DeclaringSyntaxReferences.FirstOrDefault();
                if (syntaxRef is null)
                {
                    continue;
                }

                var doc = solution.GetDocument(syntaxRef.SyntaxTree);
                if (doc is null)
                {
                    continue;
                }

                var editor = await GetEditor(doc.Id).ConfigureAwait(false);
                if (await syntaxRef.GetSyntaxAsync(cancellationToken).ConfigureAwait(false) is ClassDeclarationSyntax classDecl)
                {
                    var updated = AddHandlerAttributeToClass(classDecl);
                    editor.ReplaceNode(classDecl, updated);
                }
            }

            foreach (var editor in editors.Values)
            {
                var changed = editor.GetChangedDocument();
                var root = await changed.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var formattedRoot = Formatter.Format(root, Formatter.Annotation, changed.Project.Solution.Workspace, cancellationToken: cancellationToken);
                solution = solution.WithDocumentSyntaxRoot(changed.Id, formattedRoot);
            }

            return solution;

            async Task<DocumentEditor> GetEditor(DocumentId documentId)
            {
                if (editors.TryGetValue(documentId, out var existing))
                {
                    return existing;
                }

                var doc = solution.GetDocument(documentId);
                var editor = await DocumentEditor.CreateAsync(doc, cancellationToken).ConfigureAwait(false);
                editors[documentId] = editor;
                return editor;
            }
        }

        static ClassDeclarationSyntax AddHandlerAttributeToClass(ClassDeclarationSyntax classDeclaration)
        {
            var attribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName("Handler"));
            var attributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute))
                .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)
                .WithAdditionalAnnotations(Formatter.Annotation);
            return classDeclaration.AddAttributeLists(attributeList);
        }

        static ClassDeclarationSyntax RemoveHandlerAttribute(
            ClassDeclarationSyntax classDeclaration,
            SemanticModel semanticModel,
            INamedTypeSymbol handlerAttributeSymbol,
            CancellationToken cancellationToken)
        {
            var updatedClass = classDeclaration;
            var listsToRemove = new List<AttributeListSyntax>();

            foreach (var list in classDeclaration.AttributeLists)
            {
                var remaining = new List<AttributeSyntax>();
                foreach (var attribute in list.Attributes)
                {
                    if (!IsHandlerAttribute(attribute, semanticModel, handlerAttributeSymbol, cancellationToken))
                    {
                        remaining.Add(attribute);
                    }
                }

                if (remaining.Count == 0)
                {
                    listsToRemove.Add(list);
                }
                else if (remaining.Count != list.Attributes.Count)
                {
                    updatedClass = updatedClass.ReplaceNode(list, list.WithAttributes(SyntaxFactory.SeparatedList(remaining)));
                }
            }

            if (listsToRemove.Count > 0)
            {
                updatedClass = updatedClass.RemoveNodes(listsToRemove, SyntaxRemoveOptions.KeepTrailingTrivia);
            }

            return updatedClass;
        }

        static bool IsHandlerAttribute(
            AttributeSyntax attributeSyntax,
            SemanticModel semanticModel,
            INamedTypeSymbol handlerAttributeSymbol,
            CancellationToken cancellationToken)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(attributeSyntax, cancellationToken);

            var ctor = symbolInfo.Symbol as IMethodSymbol ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();

            return ctor is not null && SymbolEqualityComparer.Default.Equals(ctor.ContainingType, handlerAttributeSymbol);
        }

        static readonly string EquivalenceKeyAdd = $"{typeof(HandlerAttributeFixer).FullName}.Add";
        static readonly string EquivalenceKeyMove = $"{typeof(HandlerAttributeFixer).FullName}.Move";
        static readonly string EquivalenceKeyRemove = $"{typeof(HandlerAttributeFixer).FullName}.Remove";
    }
}