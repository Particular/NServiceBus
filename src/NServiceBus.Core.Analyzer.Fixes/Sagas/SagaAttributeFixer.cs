namespace NServiceBus.Core.Analyzer.Fixes;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SagaAttributeFixer))]
public class SagaAttributeFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticIds.SagaAttributeMissing, DiagnosticIds.SagaAttributeMisplaced, DiagnosticIds.SagaAttributeOnNonSaga];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        foreach (var diagnostic in context.Diagnostics)
        {
            if (root?.FindNode(context.Span, getInnermostNodeForTie: true) is not { } node)
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
                case DiagnosticIds.SagaAttributeMissing:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Add SagaAttribute",
                            token => AddSagaAttribute(context.Document, classDecl, token),
                            EquivalenceKeyAdd),
                        diagnostic);
                    break;
                case DiagnosticIds.SagaAttributeMisplaced:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Move SagaAttribute to concrete sagas",
                            token => MoveSagaAttribute(context.Document, classDecl, token),
                            EquivalenceKeyMove),
                        diagnostic);
                    break;
                case DiagnosticIds.SagaAttributeOnNonSaga:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Remove SagaAttribute",
                            token => RemoveSagaAttribute(context.Document, classDecl, token),
                            EquivalenceKeyRemove),
                        diagnostic);
                    break;
                default:
                    break;
            }
        }
    }

    static async Task<Document> AddSagaAttribute(
        Document document,
        ClassDeclarationSyntax classDeclaration,
        CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var sagaAttribute = editor.SemanticModel.Compilation.GetTypeByMetadataName("NServiceBus.SagaAttribute");
        if (sagaAttribute is null)
        {
            return document;
        }

        var updatedClass = AddAttributeToClass(classDeclaration, editor.Generator, sagaAttribute);
        editor.ReplaceNode(classDeclaration, updatedClass);

        return editor.GetChangedDocument();
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

        var updatedClass = RemoveSagaAttribute(classDeclaration, semanticModel, editor.Generator, sagaAttribute, cancellationToken);
        editor.ReplaceNode(classDeclaration, updatedClass);

        return editor.GetChangedDocument();
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

        var solutionEditor = new SolutionEditor(solution);

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

        foreach (var sagaType in nonLeafSagasWithAttribute)
        {
            foreach (var syntaxRef in sagaType.DeclaringSyntaxReferences)
            {
                var doc = solution.GetDocument(syntaxRef.SyntaxTree);
                if (doc is null)
                {
                    continue;
                }

                var editor = await solutionEditor.GetDocumentEditorAsync(doc.Id, cancellationToken).ConfigureAwait(false);
                if (await syntaxRef.GetSyntaxAsync(cancellationToken).ConfigureAwait(false) is ClassDeclarationSyntax classDecl)
                {
                    var updated = RemoveSagaAttribute(classDecl, editor.SemanticModel, editor.Generator, sagaAttribute, cancellationToken);
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

            var editor = await solutionEditor.GetDocumentEditorAsync(doc.Id, cancellationToken).ConfigureAwait(false);
            if (await syntaxRef.GetSyntaxAsync(cancellationToken).ConfigureAwait(false) is ClassDeclarationSyntax classDecl)
            {
                var updated = AddAttributeToClass(classDecl, editor.Generator, sagaAttribute);
                editor.ReplaceNode(classDecl, updated);
            }
        }

        return solutionEditor.GetChangedSolution();
    }

    static ClassDeclarationSyntax AddAttributeToClass(
        ClassDeclarationSyntax classDeclaration,
        SyntaxGenerator generator,
        INamedTypeSymbol attributeSymbol) =>
        (ClassDeclarationSyntax)generator.AddAttributes(
            classDeclaration,
            generator.Attribute(generator.TypeExpression(attributeSymbol))
                .WithAdditionalAnnotations(Formatter.Annotation, Simplifier.AddImportsAnnotation));

    static ClassDeclarationSyntax RemoveSagaAttribute(
        ClassDeclarationSyntax classDeclaration,
        SemanticModel semanticModel,
        SyntaxGenerator generator,
        INamedTypeSymbol handlerAttributeSymbol,
        CancellationToken cancellationToken)
    {
        var attributesToRemove = classDeclaration.AttributeLists
            .SelectMany(list => list.Attributes)
            .Where(attribute => IsSagaAttribute(attribute, semanticModel, handlerAttributeSymbol, cancellationToken))
            .ToList();

        var updatedClass = classDeclaration.TrackNodes(attributesToRemove);
        foreach (var attribute in attributesToRemove)
        {
            var currentAttribute = updatedClass.GetCurrentNode(attribute);

            if (currentAttribute is not null)
            {
                updatedClass = (ClassDeclarationSyntax)generator.RemoveNode(updatedClass, currentAttribute);
            }
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
    static readonly string EquivalenceKeyAdd = $"{typeof(SagaAttributeFixer).FullName}.Add";
    static readonly string EquivalenceKeyRemove = $"{typeof(SagaAttributeFixer).FullName}.Remove";
}