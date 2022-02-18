namespace NServiceBus.Core.Analyzer
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SagaAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            SagaDiagnostics.NonMappingExpressionUsedInConfigureHowToFindSaga,
            SagaDiagnostics.SagaMappingExpressionCanBeSimplified,
            SagaDiagnostics.MultipleCorrelationIdValues,
            SagaDiagnostics.MessageStartsSagaButNoMapping,
            SagaDiagnostics.SagaDataPropertyNotWriteable,
            SagaDiagnostics.MessageMappingNotNeededForTimeout,
            SagaDiagnostics.CannotMapToSagasIdProperty,
            SagaDiagnostics.DoNotUseMessageTypeAsSagaDataProperty,
            SagaDiagnostics.CorrelationIdMustBeSupportedType,
            SagaDiagnostics.EasierToInheritContainSagaData,
            SagaDiagnostics.SagaReplyShouldBeToOriginator,
            SagaDiagnostics.SagaShouldNotHaveIntermediateBaseClass,
            SagaDiagnostics.SagaShouldNotImplementNotFoundHandler,
            SagaDiagnostics.ToSagaMappingMustBeToAProperty,
            SagaDiagnostics.CorrelationPropertyTypeMustMatchMessageMappingExpressions,
            SagaDiagnostics.SagaMappingExpressionCanBeRewritten);

        public override void Initialize(AnalysisContext context) =>
            context.WithDefaultSettings().RegisterCompilationStartAction(Analyze);

        static void Analyze(CompilationStartAnalysisContext startContext)
        {
            var knownTypes = new KnownTypes(startContext.Compilation);

            if (!knownTypes.IsValid())
            {
                return;
            }

            startContext.RegisterSyntaxNodeAction(context => Analyze(context, knownTypes), SyntaxKind.ClassDeclaration);
        }

        static void Analyze(SyntaxNodeAnalysisContext context, KnownTypes knownTypes)
        {
            // Casting what should be guaranteed by the analyzer anyway
            if (!(context.Node is ClassDeclarationSyntax classDeclaration) || !(context.ContainingSymbol is INamedTypeSymbol classType))
            {
                return;
            }

            // You can't make a red squiggly outside of the node you said you were going to analyze, so we have to analyze Saga/SagaData separately
            if (classType.Implements(knownTypes.IContainSagaData))
            {
                AnalyzeSagaDataClass(context, classDeclaration, classType, knownTypes);
            }
            else if (classType.BaseTypesAndSelf().Contains(knownTypes.BaseSaga))
            {
                AnalyzeSagaClass(context, classDeclaration, classType, knownTypes);
            }
        }

        static void AnalyzeSagaClass(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax classDeclaration, INamedTypeSymbol sagaType, KnownTypes knownTypes)
        {
            // Abstract class can't be used directly anyway and anonymous type ... doubt that's even possible but let's ignore that too
            if (sagaType.IsAbstract || sagaType.IsAnonymousType)
            {
                return;
            }

            // SQL Persistence's SqlSaga<TSagaData> is a different animal. We're not analyzing those.
            if (sagaType.BaseTypesAndSelf().Any(type => type.ContainingNamespace.ToString() == "NServiceBus.Persistence.Sql" && type.Name == "SqlSaga"))
            {
                return;
            }

            // Checking for the saga to not have an intermediate base class. Remembering partial classes => separate class declarations, want to find
            // the declaration that has the base type in its inheritance list
            if (classDeclaration.BaseList != null && sagaType.BaseType.ConstructedFrom != knownTypes.GenericSaga)
            {
                foreach (var baseTypeSyntax in classDeclaration.BaseList.Types)
                {
                    var baseType = context.SemanticModel.GetTypeInfo(baseTypeSyntax.Type, context.CancellationToken).Type as INamedTypeSymbol;

                    if (baseType?.IsAssignableTo(knownTypes.BaseSaga) ?? false)
                    {
                        if (baseType?.ConstructedFrom != knownTypes.GenericSaga)
                        {
                            var diagnostic = Diagnostic.Create(SagaDiagnostics.SagaShouldNotHaveIntermediateBaseClass, baseTypeSyntax.GetLocation());
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }

            if (!TryGetSagaDetails(context, knownTypes, sagaType, out var saga))
            {
                return;
            }

            // Check to see if saga implements IHandleSagaNotFound
            if (sagaType.IsAssignableTo(knownTypes.IHandleSagaNotFound))
            {
                // Only report diagnostic if it's on THIS partial class, so BaseList must not be null
                if (classDeclaration.BaseList != null)
                {
                    var badSyntaxes = classDeclaration.BaseList.Types
                        .Where(baseType => baseType.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(idName => idName.Identifier.ValueText == "IHandleSagaNotFound"))
                        .Where(baseType => context.SemanticModel.GetTypeInfo(baseType.Type, context.CancellationToken).Type == knownTypes.IHandleSagaNotFound);

                    foreach (var badSyntax in badSyntaxes)
                    {
                        var diagnostic = Diagnostic.Create(SagaDiagnostics.SagaShouldNotImplementNotFoundHandler, badSyntax.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }

            // Is the user trying to map to more than one correlation id?
            var correlationIdGroups = saga.MessageMappings.GroupBy(m => m.CorrelationId).ToImmutableArray();
            var assumedCorrelationId = correlationIdGroups.FirstOrDefault()?.FirstOrDefault()?.CorrelationId ?? "CorrelationPropertyName";

            // Does this partial class contain the ConfigureHowToFind method?
            if (context.ContainsSyntax(saga.ConfigureHowToFindMethod))
            {
                if (correlationIdGroups.Length > 1)
                {
                    // In the case of multiple corrleation ids, want to pick one (first) as the "legit" one and then
                    // warn on all the others
                    foreach (var group in correlationIdGroups.Skip(1))
                    {
                        foreach (var mapping in group)
                        {
                            var diagnostic = Diagnostic.Create(SagaDiagnostics.MultipleCorrelationIdValues, mapping.ToSagaSyntax.GetLocation());
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
                else if (saga.MessageMappings.Select(m => m.ToSagaSyntax).Distinct().Count() > 1)
                {
                    Diagnostic diagnostic = CreateMappingRewritingDiagnostic(
                        fixerTitle: "Simplify saga mapping expression",
                        descriptor: SagaDiagnostics.SagaMappingExpressionCanBeSimplified,
                        location: saga.ConfigureHowToFindMethod.Identifier.GetLocation(),
                        newMappingForLocation: null,
                        correlationId: assumedCorrelationId,
                        saga: saga);
                    context.ReportDiagnostic(diagnostic);
                }
                else
                {
                    var mappingMethodNames = saga.MessageMappings.Select(m => (m.MessageTypeSyntax?.Parent?.Parent as GenericNameSyntax)?.Identifier.ValueText);
                    if (mappingMethodNames.Any(name => name == "ConfigureMapping" || name == "ConfigureHeaderMapping"))
                    {
                        Diagnostic diagnostic = CreateMappingRewritingDiagnostic(
                            fixerTitle: "Rewrite saga mapping expression",
                            descriptor: SagaDiagnostics.SagaMappingExpressionCanBeRewritten,
                            location: saga.ConfigureHowToFindMethod.Identifier.GetLocation(),
                            newMappingForLocation: null,
                            correlationId: assumedCorrelationId,
                            saga: saga);
                        context.ReportDiagnostic(diagnostic);
                    }
                }

                foreach (var mapping in saga.MessageMappings)
                {
                    // Warn on any mappings that are trying to map a timeout, which is unnecessary
                    if (saga.Timeouts.Any(timeoutDeclaration => timeoutDeclaration.MessageType == mapping.MessageType))
                    {
                        // Unless a timeout type is also a handler mapping for some reason
                        if (!saga.StartedBy.Any(dec => dec.MessageType == mapping.MessageType) && !saga.Handles.Any(dec => dec.MessageType == mapping.MessageType))
                        {
                            var diagnostic = Diagnostic.Create(SagaDiagnostics.MessageMappingNotNeededForTimeout, mapping.MessageTypeSyntax.GetLocation(),
                                mapping.MessageTypeSyntax.ToFullString());
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }

                // Looking for ToSaga mappings. If using new syntax, while there's only one MapSaga expression, it is copied
                // to each message mapping represented here, so we need to dedupe them by grouping on the ToSaga syntax
                foreach (var toSagaSyntaxGroup in saga.MessageMappings.GroupBy(mapping => mapping.ToSagaSyntax))
                {
                    var toSagaSyntax = toSagaSyntaxGroup.Key;
                    var firstMapping = toSagaSyntaxGroup.First();

                    // Looking for ToSaga mappings mapped to an Id property
                    // We don't care about case, becuase for example SQL persistence is case insensitive on the column name
                    if (string.Equals("Id", firstMapping.CorrelationId, StringComparison.OrdinalIgnoreCase))
                    {
                        var diagnostic = Diagnostic.Create(SagaDiagnostics.CannotMapToSagasIdProperty, toSagaSyntax.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }

                    // ToSaga mapping needs to point to a saga data property. This is also a runtime startup error.
                    var toSagaSymbol = context.SemanticModel.GetSymbolInfo(toSagaSyntax.Body, context.CancellationToken).Symbol;
                    if (toSagaSymbol is IPropertySymbol toSagaPropertySymbol)
                    {
                        foreach (var mapping in toSagaSyntaxGroup.Where(m => !m.IsHeader))
                        {
                            if (mapping.MessageMappingExpression.Expression is LambdaExpressionSyntax toMessageLambdaSyntax)
                            {
                                var toMessageSymbol = context.SemanticModel.GetSymbolInfo(toMessageLambdaSyntax.Body, context.CancellationToken).Symbol;
                                if (toMessageSymbol is IPropertySymbol toMessagePropertySymbol)
                                {
                                    if (toMessagePropertySymbol.GetMethod.ReturnType != toSagaPropertySymbol.GetMethod.ReturnType)
                                    {
                                        var diagnostic = Diagnostic.Create(SagaDiagnostics.CorrelationPropertyTypeMustMatchMessageMappingExpressions, mapping.MessageMappingExpression.GetLocation(),
                                            toMessagePropertySymbol.ContainingType.Name, toMessagePropertySymbol.Name, toMessagePropertySymbol.GetMethod.ReturnType.Name,
                                            toSagaPropertySymbol.ContainingType.Name, toSagaPropertySymbol.Name, toSagaPropertySymbol.GetMethod.ReturnType.Name);
                                        context.ReportDiagnostic(diagnostic);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        var diagnostic = Diagnostic.Create(SagaDiagnostics.ToSagaMappingMustBeToAProperty, toSagaSyntax.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }

            // Figure out which message types have a ConfigureHowToFindSaga mapping...
            var mappedMessageTypes = saga.MessageMappings
                .Select(m => context.SemanticModel.GetTypeInfo(m.MessageTypeSyntax).Type)
                .ToImmutableHashSet();

            // ...then find the IAmStartedBy message types that don't already have a mapping defined
            foreach (var declaration in saga.StartedBy.Where(declaration => context.ContainsSyntax(declaration.Syntax)))
            {
                if (!mappedMessageTypes.Contains(declaration.MessageType))
                {
                    // Worst case, we use this descriptive property name which won't compile but will hint at the developer what to do
                    var newMapping = "MessagePropertyWithCorrelationValue";

                    // If the CorrelationId property name already matches a property name in the message being mapped, we can be helpful and supply it
                    if (declaration.MessageType.GetMembers(assumedCorrelationId).FirstOrDefault() is IPropertySymbol)
                    {
                        newMapping = assumedCorrelationId;
                    }

                    var diagnostic = CreateMappingRewritingDiagnostic(
                        fixerTitle: "Create missing message mapping",
                        descriptor: SagaDiagnostics.MessageStartsSagaButNoMapping,
                        location: declaration.Syntax.GetLocation(),
                        newMappingForLocation: newMapping,
                        correlationId: assumedCorrelationId,
                        saga: saga,
                        saga.SagaType.Name, declaration.MessageType.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }

            // Look for context.Reply() and suggest saga.ReplyToOriginator
            // No fixer for this because context.Reply() has multiple overloads with message/messageConstructor and optional ReplyOptions,
            // while saga.ReplyToOriginator(object message) is the only overload for that.
            foreach (var invocationExpression in classDeclaration.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (invocationExpression.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Name.Identifier.ValueText == "Reply")
                {
                    var expressionSymbol = context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken);
                    if (expressionSymbol.Type?.IsAssignableTo(knownTypes.IMessageHandlerContext) ?? false)
                    {
                        var diagnostic = Diagnostic.Create(SagaDiagnostics.SagaReplyShouldBeToOriginator, invocationExpression.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        static void AnalyzeSagaDataClass(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax sagaDataClass, INamedTypeSymbol sagaDataType, KnownTypes knownTypes)
        {
            // First we can do some analysis without needing to look at semantic model

            // Check for directly implementing IContainSagaData and suggest ContainSagaData instead
            // Need to check base list for null, because a partial class may not have one
            if (sagaDataClass.BaseList != null)
            {
                foreach (var baseTypeSyntax in sagaDataClass.BaseList.Types)
                {
                    if (baseTypeSyntax.Type.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(s => s.Identifier.ValueText == "IContainSagaData"))
                    {
                        var diagnostic = Diagnostic.Create(SagaDiagnostics.EasierToInheritContainSagaData, baseTypeSyntax.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }

            // Go through the members and look at each property to make sure it's settable for deserialization
            // Note we don't need to check for a public parameterless constructor, the constraint on Saga `where TSagaData : class, IContainSagaData, new()` ensures that
            foreach (var memberSyntax in sagaDataClass.Members)
            {
                if (memberSyntax is PropertyDeclarationSyntax property)
                {
                    var propertyName = property.Identifier.ValueText;
                    var propertySymbol = context.SemanticModel.GetDeclaredSymbol(property, context.CancellationToken);
                    if (propertySymbol.IsReadOnly || propertySymbol.SetMethod.DeclaredAccessibility != Accessibility.Public)
                    {
                        var diagnostic = Diagnostic.Create(SagaDiagnostics.SagaDataPropertyNotWriteable, property.Identifier.GetLocation(), sagaDataType.Name, propertyName);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }

            // The only way to get saga metadata is to find the saga, and for that we unfortunately have to "visit" all symbols
            var sagaFinder = new FindSagaByDataSymbolVisitor(sagaDataType, knownTypes.GenericSaga);
            sagaFinder.Visit(context.Compilation.GlobalNamespace);
            var sagaType = sagaFinder.FoundSaga;

            if (sagaType == null)
            {
                return;
            }

            if (!TryGetSagaDetails(context, knownTypes, sagaType, out var saga))
            {
                return;
            }

            var assumedCorrelationId = saga.MessageMappings.FirstOrDefault(m => m.CorrelationId != null)?.CorrelationId;

            // In the case of partials, this is only the partial declaration currently under scrutiny
            // Other invocations of the analyzer will cover other partial classes
            // So we don't need to call `context.ContainsSyntax(memberSyntax)` here
            foreach (var memberSyntax in sagaDataClass.Members)
            {
                if (memberSyntax is PropertyDeclarationSyntax property)
                {
                    var propertyName = property.Identifier.ValueText;
                    var propertySymbol = context.SemanticModel.GetDeclaredSymbol(property, context.CancellationToken);
                    var propertyType = propertySymbol.Type;

                    // Just stuffing a message type into saga data is bad.
                    if (TypeContainsMessageType(propertyType, saga, knownTypes))
                    {
                        var diagnostic = Diagnostic.Create(SagaDiagnostics.DoNotUseMessageTypeAsSagaDataProperty, property.Type.GetLocation(), propertySymbol.Type.Name);
                        context.ReportDiagnostic(diagnostic);
                    }

                    // Hint that it's best practice for the correlation id to be a string
                    if (propertyName == assumedCorrelationId && !IsSupportedCorrelationPropertyType(propertySymbol.Type))
                    {
                        var diagnostic = Diagnostic.Create(SagaDiagnostics.CorrelationIdMustBeSupportedType, property.Type.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }


        /// <summary>
        /// The list of supported types is defined in NServiceBus.Sagas.SagaMetadata.AllowedCorrelationPropertyTypes
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0010:Add missing cases", Justification = "Don't want to add the other 30-something cases")]
        static bool IsSupportedCorrelationPropertyType(ITypeSymbol type)
        {
            // Strings and numbers are covered by SpecialType
            switch (type.SpecialType)
            {
                case SpecialType.System_String:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                    return true;
            }

            // Only case left is Guid
            return type.ContainingNamespace.Name == "System" && type.Name == "Guid";
        }

        static bool TypeContainsMessageType(ITypeSymbol type, SagaDetails saga, KnownTypes knownTypes)
        {
            if (saga.MessageTypesHandled.Contains(type))
            {
                return true;
            }

            if (type is IArrayTypeSymbol arrayType && saga.MessageTypesHandled.Contains(arrayType.ElementType))
            {
                return true;
            }

            if (type is INamedTypeSymbol asNamedType)
            {
                var enumerableType = asNamedType.ConstructedFrom == knownTypes.IEnumerableT
                ? asNamedType
                : asNamedType.AllInterfaces.FirstOrDefault(i => i.IsGenericType && i.ConstructedFrom == knownTypes.IEnumerableT);

                if (enumerableType != null)
                {
                    if (enumerableType.TypeArguments.Any(typeArg => saga.MessageTypesHandled.Contains(typeArg)))
                    {
                        return true;
                    }
                }

                // Traverse into nested generics
                if (asNamedType.TypeArguments != null)
                {
                    foreach (var typeArg in asNamedType.TypeArguments)
                    {
                        if (TypeContainsMessageType(typeArg, saga, knownTypes))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Any diagnostic created by this method can be handled by the fixer <see cref="RewriteConfigureHowToFindSagaFixer"/>.
        /// </summary>
        static Diagnostic CreateMappingRewritingDiagnostic(
            string fixerTitle,
            DiagnosticDescriptor descriptor,
            Location location,
            string newMappingForLocation,
            string correlationId,
            SagaDetails saga,
            params object[] messageArgs)
        {
            // A diagnostic can only contain the location of the code to put the red squiggly on, an array of "other locations",
            // and a string dictionary. We'd love to transmit all our saga metadata through to the fixer but we can't do that
            // directly, so we have to be creative. We'll take the number of mappings and construct a Location array of Length * 2.
            // This array will contain the location of the message type (i.e. OrderPlaced) in the even indexes, and the
            // location of the message mapping expression (i.e. msg => msg.OrderId) in the odd indexes. The rest of the data,
            // including the number of mappings, will go in the property dictionary. One additional - the ConfigureHowToFindMessages
            // method location, will be transmitted as an int. As it has a well known shape, on the Fixer end we can look up the token
            // at that location and then find the associated MethodDeclarationSyntax easily enough.
            // Note: All properties should be prefixed with `_` because we'll also be storing whether each message mapping is
            // a header mapping by using keys that are index integers converted to strings. (See below.)
            var properties = new Dictionary<string, string>
                {
                    { "_FixerTitle", fixerTitle },
                    { "_CorrelationId", correlationId },
                    { "_MapperParamName", saga.MapperParameterSyntax.Identifier.ValueText },
                    { "_MappingCount", saga.MessageMappings.Count.ToString() },
                    { "_ConfigureHowToFindLocation", saga.ConfigureHowToFindMethod.GetLocation().SourceSpan.Start.ToString() },
                };

            // If we are generating a new mapping for a message type, we get the message type out of the IAmStartedByMessages<T>
            // that is in the primary location (it will be under the red squiggly) and we can extract the T parameter and use that.
            // The property name to be mapped is sent as a string, and the expression `msg => msg.NewPropertyName` is generated
            // in the fixer.
            if (newMappingForLocation != null)
            {
                properties["_NewMappingForLocation"] = newMappingForLocation;
            }

            // Create the double-size location array to hold the message type & message mapping syntaxes
            var expressionLocations = new Location[saga.MessageMappings.Count * 2];

            for (int i = 0; i < saga.MessageMappings.Count; i++)
            {
                var mapping = saga.MessageMappings[i];
                var by2Index = i * 2;
                expressionLocations[by2Index] = mapping.MessageTypeSyntax.GetLocation();
                expressionLocations[by2Index + 1] = mapping.MessageMappingExpression.GetLocation();

                // Also want to know whether each message mapping expression is a header expression. Given limited options,
                // we just shove a stringified boolean into the string properties, i.e. { "0" => "True" } means that the first
                // message mapping is a ToMessageHeader mapping. The rest of the properties are prefixed with `_` to keep things
                // clear.
                properties[i.ToString()] = mapping.IsHeader.ToString();
            }

            var diagnostic = Diagnostic.Create(descriptor, location, expressionLocations, properties.ToImmutableDictionary(), messageArgs);
            return diagnostic;
        }

        static bool TryGetSagaDetails(SyntaxNodeAnalysisContext context, KnownTypes knownTypes, INamedTypeSymbol sagaType, out SagaDetails saga)
        {
            saga = null;

            // If we are visiting from a SagaData in a different file, we need to use a different semantic model

            // Find all places where the saga class is "declared" which could be one place or could be multiple partial classes
            var classDeclarations = sagaType.DeclaringSyntaxReferences
                .Select(reference => reference.GetSyntax(context.CancellationToken))
                .OfType<ClassDeclarationSyntax>()
                .ToImmutableArray();

            // From the potential partial classes, find the ConfigureHowToFindSaga method in one of them
            var configureHowToFindMethod = classDeclarations
                .SelectMany(cls => cls.Members.OfType<MethodDeclarationSyntax>())
                .FirstOrDefault(method => method.Identifier.ValueText == "ConfigureHowToFindSaga");

            if (configureHowToFindMethod == null)
            {
                return false;
            }

            var sagaDataType = GetSagaDataType(sagaType, knownTypes);
            if (sagaDataType == null)
            {
                return false;
            }

            // From all the base lists on all partials, find the methods that are one of our IAmStarted/IHandle methods, then get
            // the TypeSymbol so we have both the syntax and type available
            var handlerDeclarations = classDeclarations
                .Where(cls => cls.BaseList != null)
                .SelectMany(cls => cls.BaseList.Types)
                .Where(s => s.DescendantNodes().OfType<SimpleNameSyntax>().Any(n => IsHandlerInterfaceName(n.Identifier.ValueText)))
                .Select(syntax =>
                {
                    var correctSemanticModel = context.Compilation.GetSemanticModel(syntax.SyntaxTree);
                    var typeSymbol = correctSemanticModel.GetTypeInfo(syntax.Type, context.CancellationToken).Type as INamedTypeSymbol;
                    return new SagaHandlerDeclaration(syntax, typeSymbol);
                })
                .ToImmutableArray();

            // Divide the handler declarations into each type: StartedBy, Handle, or Timeout

            var handles = handlerDeclarations
                .Where(d => d.InterfaceType.IsGenericType && d.InterfaceType.ConstructedFrom == knownTypes.IHandleMessages)
                .Where(d => d.MessageType != null)
                .ToImmutableArray();

            var startedBy = handlerDeclarations
                .Where(d => d.InterfaceType.IsGenericType && d.InterfaceType.ConstructedFrom == knownTypes.IAmStartedByMessages)
                .Where(d => d.MessageType != null)
                .ToImmutableArray();

            var timeouts = handlerDeclarations
                .Where(d => d.InterfaceType.IsGenericType && d.InterfaceType.ConstructedFrom == knownTypes.IHandleTimeouts)
                .Where(d => d.MessageType != null)
                .ToImmutableArray();

            saga = new SagaDetails(sagaType, sagaDataType, configureHowToFindMethod, handles, startedBy, timeouts);

            // Delve into the ConfigureHowToFindSaga details to try to get mapping details out
            TryFillSagaMappings(context, knownTypes, saga);

            return true;
        }

        static void TryFillSagaMappings(SyntaxNodeAnalysisContext context, KnownTypes knownTypes, SagaDetails saga)
        {
            // Want to know what the "mapper" is if the user has renamed it
            saga.MapperParameterSyntax = saga.ConfigureHowToFindMethod.ParameterList.Parameters.SingleOrDefault();
            if (saga.MapperParameterSyntax == null)
            {
                return;
            }

            // Get the semantic model for wherever the ConfigureHowToFindSaga method lives
            var correctSemanticModel = context.Compilation.GetSemanticModel(saga.ConfigureHowToFindMethod.SyntaxTree);

            // Make sure the param of ConfigureHowToFindSaga is a SagaPropertyMapper<T> where T matches the saga data type
            // Otherwise the user is trying to troll us with another very similar method
            if (!(correctSemanticModel.GetTypeInfo(saga.MapperParameterSyntax.Type, context.CancellationToken).Type is INamedTypeSymbol mapperType) || !mapperType.IsGenericType || mapperType.ConstructedFrom != knownTypes.SagaPropertyMapper || mapperType.TypeArguments.SingleOrDefault() != saga.DataType)
            {
                return;
            }

            // This is maybe a code smell, but we raise diagnostics from non-mapping expressions as we go through them rather than in the Analyze methods
            // above. But if we're parsing through this data while analyzing a SagaData class, we don't want to double-raise diagnostics that we
            // aren't able to make red squigglies for. If this evaluates false, we are just collecting data, not creating diagnostics.
            bool raiseDiagnostics = context.ContainsSyntax(saga.ConfigureHowToFindMethod);

            if (saga.ConfigureHowToFindMethod.Body != null) // ConfigureHowToFindSaga method is a method block with { }
            {
                foreach (var childNode in saga.ConfigureHowToFindMethod.Body.ChildNodes())
                {
                    AddSagaMappingsFromExpression(context, correctSemanticModel, childNode, saga, raiseDiagnostics);
                }
            }
            else if (saga.ConfigureHowToFindMethod.ExpressionBody != null) // ConfigureHowToFindSaga method is an arrow expression => with one mapping expression
            {
                // There is only one expression in an arrow expression, so scan it with expressionIndex of 0
                AddSagaMappingsFromExpression(context, correctSemanticModel, saga.ConfigureHowToFindMethod.ExpressionBody.Expression, saga, raiseDiagnostics);
            }
        }

        static void AddSagaMappingsFromExpression(SyntaxNodeAnalysisContext context, SemanticModel semanticModel, SyntaxNode statement, SagaDetails saga, bool raiseDiagnostics)
        {
            // If we fail to add a mapping from an expression, then it's not a mapping expression, and we raise a diagnostic (if enabled)
            if (!TryAddSagaMappingsFromExpression(semanticModel, statement, saga, context.CancellationToken) && raiseDiagnostics)
            {
                var diagnostic = Diagnostic.Create(SagaDiagnostics.NonMappingExpressionUsedInConfigureHowToFindSaga, statement.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        static bool TryAddSagaMappingsFromExpression(SemanticModel semanticModel, SyntaxNode statement, SagaDetails saga, CancellationToken cancellationToken)
        {
            // Normalize depending on whether we're looking at an arrow statement or a regular statement expression inside a block
            if (statement is ExpressionStatementSyntax expression)
            {
                statement = expression.Expression;
            }

            // All mapping expressions will be an invocation, either mapper.MapSaga() or mapper.ConfigureMapping(), etc.
            if (!(statement is InvocationExpressionSyntax invocation))
            {
                return false;
            }

            // Chained statements (mapper.MapSaga().ToMessage().ToMessage() etc) are represented backwards or inside-out in the expression tree
            // BUT don't descend into LambdaExpressionSyntax because those are the mapping expressions themselves and COULD (but shouldn't?) have invocations as well
            var invokeChain = invocation.DescendantNodesAndSelf(currentNode => !(currentNode is LambdaExpressionSyntax))
                .OfType<InvocationExpressionSyntax>()
                .Reverse()
                .ToImmutableArray();

            // This represents the mapper.ConfigureMapping() or the mapper.MapSaga() call at the beginning
            var firstInvoke = invokeChain.FirstOrDefault();

            // Expression needs to be a member access to be mapper.Something()
            if (firstInvoke == null || !(firstInvoke.Expression is MemberAccessExpressionSyntax memberAccess))
            {
                return false;
            }

            // Make sure the left side of the expression is the mapper variable name
            if (!(memberAccess.Expression is IdentifierNameSyntax identifierName) || identifierName.Identifier.ValueText != saga.MapperParameterSyntax.Identifier.ValueText)
            {
                return false;
            }

            // either old syntax ConfigureMapping/ConfigureHeaderMapping, or newer MapSaga
            var firstInvocationMethodName = memberAccess.Name.Identifier.ValueText;

            if (firstInvocationMethodName == "ConfigureMapping" || firstInvocationMethodName == "ConfigureHeaderMapping") // if old syntax
            {
                // Method has to be generic so we can get the generic type, which is the message type
                if (!(memberAccess.Name is GenericNameSyntax genericTypeName))
                {
                    return false;
                }

                // Get the generic argument of the Configure... method, this is the message type being mapped
                var messageTypeSyntax = genericTypeName.TypeArgumentList.Arguments.FirstOrDefault();
                if (messageTypeSyntax == null)
                {
                    return false;
                }

                // Get the named type for the message type
                var messageTypeInfo = semanticModel.GetTypeInfo(messageTypeSyntax, cancellationToken);
                if (!(messageTypeInfo.Type is INamedTypeSymbol messageType))
                {
                    return false;
                }

                // Whether the argument is a literal string for a header, or a `msg => msg.PropertyName`, we don't care we just want the argument in its entirety
                var headerOrMessageExpressionArgument = firstInvoke.ArgumentList.Arguments.FirstOrDefault();
                if (headerOrMessageExpressionArgument == null)
                {
                    return false;
                }

                // The ToSaga() expression is the 2nd invocation in the chain
                var toSagaInvocation = invokeChain.Skip(1).SingleOrDefault();
                if ((toSagaInvocation.Expression as MemberAccessExpressionSyntax)?.Name?.Identifier.ValueText != "ToSaga")
                {
                    return false;
                }

                // The argument of the ToSaga() method is the ToSaga mapping expression `saga => saga.OrderId`
                if (!(toSagaInvocation.ArgumentList.Arguments.FirstOrDefault()?.Expression is LambdaExpressionSyntax toSagaSyntax))
                {
                    return false;
                }

                var isHeaderMapping = firstInvocationMethodName == "ConfigureHeaderMapping";
                var mapping = new SagaMessageMapping(messageTypeSyntax, messageType, isHeaderMapping, headerOrMessageExpressionArgument, toSagaSyntax);
                saga.MessageMappings.Add(mapping);
            }
            else if (firstInvocationMethodName == "MapSaga") // New syntax
            {
                // Looking for the lambda expression `saga => saga.OrderId` inside the argument of MapSaga()
                if (!(firstInvoke.ArgumentList.Arguments.FirstOrDefault()?.Expression is SimpleLambdaExpressionSyntax toSagaSyntax))
                {
                    return false;
                }

                // Iterate through the fluent .ToMessage() or .ToMessageHeader() calls
                foreach (var fluentInvocation in invokeChain.Skip(1))
                {
                    // Either way it needs to be a MemberAccess: PreviousSomething.SomethingElse
                    if (!(fluentInvocation.Expression is MemberAccessExpressionSyntax toSomethingMemberAccess))
                    {
                        return false;
                    }

                    // Method has to be generic so we can get the generic type, which is the message type
                    if (!(toSomethingMemberAccess.Name is GenericNameSyntax genericTypeName))
                    {
                        return false;
                    }

                    // Now we get the syntax for the message type
                    var messageTypeSyntax = genericTypeName.TypeArgumentList.Arguments.FirstOrDefault();
                    if (messageTypeSyntax == null)
                    {
                        return false;
                    }

                    // And translate the message type syntax to the Type it represents
                    var messageTypeInfo = semanticModel.GetTypeInfo(messageTypeSyntax, cancellationToken);
                    if (!(messageTypeInfo.Type is INamedTypeSymbol messageType))
                    {
                        return false;
                    }

                    // Whether the argument is a simple string for a header, or a `msg => msg.PropertyName`, we don't care we just want the argument
                    var headerOrMessageExpressionArgument = fluentInvocation.ArgumentList.Arguments.FirstOrDefault();
                    if (headerOrMessageExpressionArgument == null)
                    {
                        return false;
                    }

                    var methodName = toSomethingMemberAccess.Name.Identifier.ValueText;
                    var isHeaderMapping = IsMessageHeaderMapping(methodName);
                    var mapping = new SagaMessageMapping(messageTypeSyntax, messageType, isHeaderMapping, headerOrMessageExpressionArgument, toSagaSyntax);
                    saga.MessageMappings.Add(mapping);
                }
            }

            return true;
        }

        static bool IsMessageHeaderMapping(string methodName)
        {
            switch (methodName)
            {
                case "ToMessage":
                case "ConfigureMapping":
                    return false;
                case "ToMessageHeader":
                case "ConfigureHeaderMapping":
                    return true;
                default:
                    throw new Exception($"Unknown if method name {methodName} denotes a message header mapping.");
            }
        }

        static INamedTypeSymbol GetSagaDataType(INamedTypeSymbol sagaType, KnownTypes knownTypes)
        {
            var genericSagaBaseType = sagaType.BaseTypesAndSelf()
                .OfType<INamedTypeSymbol>()
                .FirstOrDefault(t => t.ConstructedFrom == knownTypes.GenericSaga);

            return genericSagaBaseType?.TypeArguments.FirstOrDefault() as INamedTypeSymbol;
        }

        static bool IsHandlerInterfaceName(string interfaceName)
        {
            switch (interfaceName)
            {
                case "IHandleMessages":
                case "IAmStartedByMessages":
                case "IHandleTimeouts":
                    return true;
                default:
                    return false;
            }
        }

        class KnownTypes
        {
            public INamedTypeSymbol BaseSaga { get; }
            public INamedTypeSymbol GenericSaga { get; }
            public INamedTypeSymbol SagaPropertyMapper { get; }
            public INamedTypeSymbol IAmStartedByMessages { get; }
            public INamedTypeSymbol IHandleMessages { get; }
            public INamedTypeSymbol IHandleTimeouts { get; }
            public INamedTypeSymbol IContainSagaData { get; }
            public INamedTypeSymbol ContainSagaData { get; }
            public INamedTypeSymbol IMessageHandlerContext { get; }
            public INamedTypeSymbol IHandleSagaNotFound { get; }
            public INamedTypeSymbol IEnumerableT { get; }

            public KnownTypes(Compilation compilation)
            {
                BaseSaga = compilation.GetTypeByMetadataName("NServiceBus.Saga");
                GenericSaga = compilation.GetTypeByMetadataName("NServiceBus.Saga`1");
                SagaPropertyMapper = compilation.GetTypeByMetadataName("NServiceBus.SagaPropertyMapper`1");
                IAmStartedByMessages = compilation.GetTypeByMetadataName("NServiceBus.IAmStartedByMessages`1");
                IHandleMessages = compilation.GetTypeByMetadataName("NServiceBus.IHandleMessages`1");
                IHandleTimeouts = compilation.GetTypeByMetadataName("NServiceBus.IHandleTimeouts`1");
                IContainSagaData = compilation.GetTypeByMetadataName("NServiceBus.IContainSagaData");
                ContainSagaData = compilation.GetTypeByMetadataName("NServiceBus.ContainSagaData");
                IMessageHandlerContext = compilation.GetTypeByMetadataName("NServiceBus.IMessageHandlerContext");
                IHandleSagaNotFound = compilation.GetTypeByMetadataName("NServiceBus.Sagas.IHandleSagaNotFound");
                IEnumerableT = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");
            }

            public bool IsValid() =>
                BaseSaga != null &&
                GenericSaga != null &&
                SagaPropertyMapper != null &&
                IAmStartedByMessages != null &&
                IHandleMessages != null &&
                IHandleTimeouts != null &&
                IContainSagaData != null &&
                ContainSagaData != null &&
                IMessageHandlerContext != null &&
                IHandleSagaNotFound != null &&
                IEnumerableT != null;
        }
    }
}
