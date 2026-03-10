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
using NServiceBus.Core.Analyzer.Handlers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(HandlerAttributeFixer))]
public class HandlerAttributeFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticIds.HandlerAttributeMissing, DiagnosticIds.HandlerAttributeMisplaced, DiagnosticIds.HandlerAttributeMissingInterfaceLess, DiagnosticIds.HandlerAttributeMisplacedInterfaceLess, DiagnosticIds.HandlerAttributeOnNonHandler];

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
                case DiagnosticIds.HandlerAttributeMissingInterfaceLess:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Add HandlerAttribute",
                            token => AddHandlerAttribute(context.Document, classDecl, token),
                            EquivalenceKeyAdd),
                        diagnostic);
                    break;
                case DiagnosticIds.HandlerAttributeMisplaced:
                case DiagnosticIds.HandlerAttributeMisplacedInterfaceLess:
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
        if (!HandlerKnownTypes.TryGet(editor.SemanticModel.Compilation, out var knownTypes))
        {
            return document;
        }

        var updatedClass = AddAttributeToClass(classDeclaration, editor.Generator, knownTypes.HandlerAttribute);
        editor.ReplaceNode(classDeclaration, updatedClass);

        return editor.GetChangedDocument();
    }

    static async Task<Document> RemoveHandlerAttribute(
        Document document,
        ClassDeclarationSyntax classDeclaration,
        CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = editor.SemanticModel;

        var compilation = semanticModel.Compilation;
        if (!HandlerKnownTypes.TryGet(compilation, out var knownTypes))
        {
            return document;
        }

        var updatedClass = RemoveHandlerAttribute(classDeclaration, semanticModel, editor.Generator, knownTypes.HandlerAttribute, cancellationToken);
        editor.ReplaceNode(classDeclaration, updatedClass);

        return editor.GetChangedDocument();
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
        if (!HandlerKnownTypes.TryGet(compilation, out var knownTypes))
        {
            return solution;
        }

        var solutionEditor = new SolutionEditor(solution);

        var allClassTypes = new List<INamedTypeSymbol>();
        var directlyHandlerTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
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

            allClassTypes.Add(type);

            if (type.ImplementsGenericType(knownTypes.SagaBase))
            {
                continue;
            }

            var isInterfaceBasedHandler = type.ImplementsGenericInterface(knownTypes.IHandleMessages);
            var isInterfaceLessHandler = !isInterfaceBasedHandler && HasValidInterfaceLessHandleMethods(type, knownTypes);

            if (!isInterfaceBasedHandler && !isInterfaceLessHandler)
            {
                continue;
            }

            directlyHandlerTypes.Add(type);
        }

        var handlerTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var type in allClassTypes)
        {
            if (type.ImplementsGenericType(knownTypes.SagaBase))
            {
                continue;
            }

            if (!IsHandlerType(type, directlyHandlerTypes))
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
            var hasAttribute = type.HasAttribute(knownTypes.HandlerAttribute);

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

        foreach (var handlerType in nonLeafHandlersWithAttribute)
        {
            foreach (var syntaxRef in handlerType.DeclaringSyntaxReferences)
            {
                var doc = solution.GetDocument(syntaxRef.SyntaxTree);
                if (doc is null)
                {
                    continue;
                }

                var editor = await solutionEditor.GetDocumentEditorAsync(doc.Id, cancellationToken).ConfigureAwait(false);
                if (await syntaxRef.GetSyntaxAsync(cancellationToken).ConfigureAwait(false) is ClassDeclarationSyntax classDecl)
                {
                    var updated = RemoveHandlerAttribute(classDecl, editor.SemanticModel, editor.Generator, knownTypes.HandlerAttribute, cancellationToken);
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

            var editor = await solutionEditor.GetDocumentEditorAsync(doc.Id, cancellationToken).ConfigureAwait(false);
            if (await syntaxRef.GetSyntaxAsync(cancellationToken).ConfigureAwait(false) is ClassDeclarationSyntax classDecl)
            {
                var updated = AddAttributeToClass(classDecl, editor.Generator, knownTypes.HandlerAttribute);
                editor.ReplaceNode(classDecl, updated);
            }
        }

        return solutionEditor.GetChangedSolution();
    }

    static bool IsHandlerType(INamedTypeSymbol type, HashSet<INamedTypeSymbol> directlyHandlerTypes)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (directlyHandlerTypes.Contains(current.OriginalDefinition))
            {
                return true;
            }
        }

        return false;
    }

    static bool HasValidInterfaceLessHandleMethods(INamedTypeSymbol classType, HandlerKnownTypes knownTypes)
    {
        var interfaceMessageTypes = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var iface in classType.AllInterfaces)
        {
            if (iface.IsGenericType &&
                IsHandlerInterface(iface.OriginalDefinition, knownTypes) &&
                iface.TypeArguments[0] is INamedTypeSymbol msgType)
            {
                interfaceMessageTypes.Add(msgType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }
        }

        foreach (var member in classType.GetMembers())
        {
            if (member is not IMethodSymbol method)
            {
                continue;
            }

            if (method.Name != "Handle" ||
                method.DeclaredAccessibility != Accessibility.Public ||
                method.MethodKind == MethodKind.ExplicitInterfaceImplementation ||
                method.Parameters.Length < 2)
            {
                continue;
            }

            if (method.Parameters[0].Type is not INamedTypeSymbol firstParamType)
            {
                continue;
            }

            var secondParam = method.Parameters[1];
            if (!SymbolEqualityComparer.Default.Equals(secondParam.Type, knownTypes.IMessageHandlerContext))
            {
                continue;
            }

            if (!IsSupportedHandlerReturnType(method.ReturnType))
            {
                continue;
            }

            var firstParamFqn = firstParamType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (method.Parameters.Length == 2 && interfaceMessageTypes.Contains(firstParamFqn))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    static bool IsHandlerInterface(INamedTypeSymbol ifaceDefinition, HandlerKnownTypes knownTypes) =>
        SymbolEqualityComparer.Default.Equals(ifaceDefinition, knownTypes.IHandleMessages) ||
        SymbolEqualityComparer.Default.Equals(ifaceDefinition, knownTypes.IHandleTimeouts) ||
        SymbolEqualityComparer.Default.Equals(ifaceDefinition, knownTypes.IAmStartedByMessages);

    static bool IsSupportedHandlerReturnType(ITypeSymbol type)
    {
        return type is INamedTypeSymbol
        {
            Name: "Task",
            ContainingNamespace: { Name: "Tasks", ContainingNamespace: { Name: "Threading", ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } } }
        };
    }

    static ClassDeclarationSyntax AddAttributeToClass(
        ClassDeclarationSyntax classDeclaration,
        SyntaxGenerator generator,
        INamedTypeSymbol attributeSymbol) =>
        (ClassDeclarationSyntax)generator.AddAttributes(
            classDeclaration,
            generator.Attribute(generator.TypeExpression(attributeSymbol))
                .WithAdditionalAnnotations(Formatter.Annotation, Simplifier.AddImportsAnnotation));

    static ClassDeclarationSyntax RemoveHandlerAttribute(
        ClassDeclarationSyntax classDeclaration,
        SemanticModel semanticModel,
        SyntaxGenerator generator,
        INamedTypeSymbol handlerAttributeSymbol,
        CancellationToken cancellationToken)
    {
        var attributesToRemove = classDeclaration.AttributeLists
            .SelectMany(list => list.Attributes)
            .Where(attribute => IsHandlerAttribute(attribute, semanticModel, handlerAttributeSymbol, cancellationToken))
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