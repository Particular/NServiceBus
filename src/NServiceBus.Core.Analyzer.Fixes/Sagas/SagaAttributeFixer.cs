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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SagaAttributeFixer))]
    public class SagaAttributeFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            [DiagnosticIds.SagaAttributeMissing, DiagnosticIds.SagaAttributeMisplaced, DiagnosticIds.SagaAttributeOnNonSaga];

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            if (root?.FindNode(context.Span, getInnermostNodeForTie: true) is not { } node)
            {
                return;
            }

            var classDecl = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (classDecl is null)
            {
                return;
            }

            if (diagnostic.Id == DiagnosticIds.SagaAttributeMissing)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Add SagaAttribute",
                        token => AddSagaAttribute(context.Document, classDecl, token),
                        EquivalenceKeyMove),
                    diagnostic);
            }
            else if (diagnostic.Id == DiagnosticIds.SagaAttributeMisplaced)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Move SagaAttribute to concrete sagas",
                        token => MoveSagaAttribute(context.Document, classDecl, token),
                        EquivalenceKeyMove),
                    diagnostic);
            }
            else if (diagnostic.Id == DiagnosticIds.SagaAttributeOnNonSaga)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Remove SagaAttribute",
                        token => RemoveSagaAttribute(context.Document, classDecl, token),
                        EquivalenceKeyMove),
                    diagnostic);
            }
        }

        static async Task<Document> AddSagaAttribute(
            Document document,
            ClassDeclarationSyntax classDeclaration,
            CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var updatedClass = AddSagaAttributeToClass(classDeclaration);
            editor.ReplaceNode(classDeclaration, updatedClass);

            var changed = editor.GetChangedDocument();
            var root = await changed.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var formattedRoot = Formatter.Format(root, Formatter.Annotation, changed.Project.Solution.Workspace, cancellationToken: cancellationToken);
            return changed.WithSyntaxRoot(formattedRoot);
        }

        static async Task<Document> RemoveSagaAttribute(
            Document document,
            ClassDeclarationSyntax classDeclaration,
            CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var semanticModel = editor.SemanticModel;

            var compilation = semanticModel.Compilation;
            // TODO cache?
            var sagaBaseType = compilation.GetTypeByMetadataName("NServiceBus.Saga`1");
            var sagaAttribute = compilation.GetTypeByMetadataName("NServiceBus.SagaAttribute");
            if (sagaBaseType is null || sagaAttribute is null)
            {
                return document;
            }

            var updatedClass = RemoveSagaAttribute(classDeclaration, semanticModel, sagaAttribute, cancellationToken);
            editor.ReplaceNode(classDeclaration, updatedClass);

            var changed = editor.GetChangedDocument();
            var root = await changed.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var formattedRoot = Formatter.Format(root, Formatter.Annotation, changed.Project.Solution.Workspace, cancellationToken: cancellationToken);
            return changed.WithSyntaxRoot(formattedRoot);
        }

        static async Task<Solution> MoveSagaAttribute(
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
            var sagaBaseType = compilation.GetTypeByMetadataName("NServiceBus.Saga`1");
            var sagaAttribute = compilation.GetTypeByMetadataName("NServiceBus.SagaAttribute");
            if (sagaBaseType is null || sagaAttribute is null)
            {
                return solution;
            }

            var sagaTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
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

                if (!type.ImplementsGenericType(sagaBaseType))
                {
                    continue;
                }

                sagaTypes.Add(type);

                if (type.BaseType is { SpecialType: not SpecialType.System_Object } baseType)
                {
                    baseTypes.Add(baseType.OriginalDefinition);
                }
            }

            var leafSagas = new List<INamedTypeSymbol>();
            var nonLeafSagasWithAttribute = new List<INamedTypeSymbol>();

            foreach (var type in sagaTypes)
            {
                var isLeaf = !type.IsAbstract && !baseTypes.Contains(type.OriginalDefinition);
                var hasAttribute = type.HasAttribute(sagaAttribute);

                if (isLeaf)
                {
                    if (!hasAttribute)
                    {
                        leafSagas.Add(type);
                    }
                }
                else if (hasAttribute)
                {
                    nonLeafSagasWithAttribute.Add(type);
                }
            }

            var editors = new Dictionary<DocumentId, DocumentEditor>();

            foreach (var handlerType in nonLeafSagasWithAttribute)
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
                        var updated = RemoveSagaAttribute(classDecl, editor.SemanticModel, sagaAttribute, cancellationToken);
                        editor.ReplaceNode(classDecl, updated);
                    }
                }
            }

            foreach (var leafSaga in leafSagas)
            {
                var syntaxRef = leafSaga.DeclaringSyntaxReferences.FirstOrDefault();
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
                    var updated = AddSagaAttributeToClass(classDecl);
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

        static ClassDeclarationSyntax AddSagaAttributeToClass(ClassDeclarationSyntax classDeclaration)
        {
            var attribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName("NServiceBus.SagaAttribute"));
            var attributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute))
                .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)
                .WithAdditionalAnnotations(Formatter.Annotation);
            return classDeclaration.AddAttributeLists(attributeList);
        }

        static ClassDeclarationSyntax RemoveSagaAttribute(
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
                    if (!IsSagaAttribute(attribute, semanticModel, handlerAttributeSymbol, cancellationToken))
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
                updatedClass = updatedClass.RemoveNodes(listsToRemove, SyntaxRemoveOptions.KeepLeadingTrivia);
            }

            return updatedClass;
        }

        static bool IsSagaAttribute(
            AttributeSyntax attributeSyntax,
            SemanticModel semanticModel,
            INamedTypeSymbol sagaAttributeSymbol,
            CancellationToken cancellationToken)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(attributeSyntax, cancellationToken);

            var ctor = symbolInfo.Symbol as IMethodSymbol ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();

            return ctor is not null && SymbolEqualityComparer.Default.Equals(ctor.ContainingType, sagaAttributeSymbol);
        }

        static readonly string EquivalenceKeyMove = $"{typeof(SagaAttributeFixer).FullName}.Move";
    }
}