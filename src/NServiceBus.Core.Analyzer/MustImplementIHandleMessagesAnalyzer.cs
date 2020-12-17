namespace NServiceBus.Core.Analyzer
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MustImplementIHandleMessagesAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        ///     Gets the list of supported diagnostics for the analyzer.
        /// </summary>
        /// <value>
        ///     The supported diagnostics.
        /// </value>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            MustImplementIHandleMessagesDiagnostic,
            MustImplementIAmStartedByMessagesDiagnostic,
            MustImplementIHandleTimeoutsDiagnostic,
            tooManyHandleMethodsDiagnostic);

        /// <summary>
        ///     Initializes the specified analyzer on the <paramref name="context" />.
        /// </summary>
        /// <param name="context">The context.</param>
        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.ClassDeclaration);

        void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is ClassDeclarationSyntax classDeclaration))
            {
                return;
            }

            if (classDeclaration.BaseList == null)
            {
                return;
            }

            foreach (var childNode in classDeclaration.BaseList.ChildNodes())
            {
                if (childNode is BaseTypeSyntax baseTypeSyntax)
                {
                    if (BaseTypeIsHandlerSignature(context, baseTypeSyntax, out string simpleInterfaceName, out var messageIdentifier))
                    {
                        var isTimeout = (simpleInterfaceName == "IHandleTimeouts");
                        if (!HasImplementationDefined(context, classDeclaration, isTimeout, messageIdentifier))
                        {
                            var location = baseTypeSyntax.GetLocation();
                            var fixerMethodName = (isTimeout ? "Timeout" : "Handle");
                            var diagnosticDescriptor = GetMustHandleDiagnostic(simpleInterfaceName);

                            var properties = new Dictionary<string, string>
                            {
                                { "MessageType", messageIdentifier },
                                { "FixerMethodName", fixerMethodName }
                            }.ToImmutableDictionary();

                            var diagnostic = Diagnostic.Create(diagnosticDescriptor, location, properties, baseTypeSyntax.ToString());
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }

        private static DiagnosticDescriptor GetMustHandleDiagnostic(string simpleInterfaceName)
        {
            switch (simpleInterfaceName)
            {
                case "IHandleMessages":
                    return MustImplementIHandleMessagesDiagnostic;

                case "IAmStartedByMessages":
                    return MustImplementIAmStartedByMessagesDiagnostic;

                case "IHandleTimeouts":
                    return MustImplementIHandleTimeoutsDiagnostic;

                default:
                    throw new Exception("Unknown handler marker interface.");
            }
        }

        private static bool BaseTypeIsHandlerSignature(SyntaxNodeAnalysisContext context, BaseTypeSyntax baseTypeSyntax, out string simpleName, out string messageIdentifier)
        {
            messageIdentifier = null;
            simpleName = null;

            var interfaceGenericNameSyntax = baseTypeSyntax.ChildNodes().OfType<GenericNameSyntax>().FirstOrDefault();
            if (interfaceGenericNameSyntax == null)
            {
                return false;
            }

            simpleName = interfaceGenericNameSyntax.Identifier.ValueText;
            if (simpleName != "IHandleMessages" && simpleName != "IAmStartedByMessages" && simpleName != "IHandleTimeouts")
            {
                return false;
            }

            var symbolInfo = context.SemanticModel.GetSymbolInfo(interfaceGenericNameSyntax);
            if (!(symbolInfo.Symbol is INamedTypeSymbol type))
            {
                return false;
            }

            if (type.ContainingNamespace.Name != "NServiceBus" || type.ContainingModule.Name != "NServiceBus.Core.dll")
            {
                return false;
            }

            messageIdentifier = type.TypeArguments[0].Name;
            return true;
        }

        private static bool HasImplementationDefined(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax classDeclaration, bool isTimeout, string messageIdentifier)
        {
            bool foundHandler = false;
            SyntaxToken firstHandler = default;
            List<SyntaxToken> additionalHandlers = null;

            foreach (var member in classDeclaration.Members)
            {
                if (member is MethodDeclarationSyntax methodDeclaration)
                {
                    if (IsMethodAHandleMethod(methodDeclaration, isTimeout, messageIdentifier))
                    {
                        if (!foundHandler)
                        {
                            foundHandler = true;
                            firstHandler = methodDeclaration.Identifier;
                        }
                        else
                        {
                            if (additionalHandlers == null)
                            {
                                additionalHandlers = new List<SyntaxToken>(3);
                            }
                            additionalHandlers.Add(methodDeclaration.Identifier);
                        }
                    }
                }
            }

            if (additionalHandlers != null)
            {
                var additionalLocations = additionalHandlers.Select(handler => handler.GetLocation());
                var baseMethodName = isTimeout ? "Timeout" : "Handle";
                var diagnostic = Diagnostic.Create(tooManyHandleMethodsDiagnostic, firstHandler.GetLocation(), additionalLocations, baseMethodName, messageIdentifier);
                context.ReportDiagnostic(diagnostic);
            }

            return foundHandler;
        }

        static bool IsMethodAHandleMethod(MethodDeclarationSyntax methodDeclaration, bool isTimeout, string messageIdentifier)
        {
            var allowableMethodNames = (isTimeout ? timeoutMethodNames : handlerMethodNames);
            var methodName = methodDeclaration.Identifier.Text;

            if (!allowableMethodNames.Contains(methodName))
            {
                return false;
            }

            var paramList = methodDeclaration.ParameterList.ChildNodes().ToImmutableArray();
            if (paramList.Length != 2 && paramList.Length != 3)
            {
                return false;
            }

            if (!(paramList[0] is ParameterSyntax msgParam) || (msgParam.Type as IdentifierNameSyntax).Identifier.ValueText != messageIdentifier)
            {
                return false;
            }

            if (!(paramList[1] is ParameterSyntax contextParam) || (contextParam.Type as IdentifierNameSyntax).Identifier.ValueText != "IMessageHandlerContext")
            {
                return false;
            }

            if (paramList.Length == 3)
            {
                if (!(paramList[2] is ParameterSyntax cancellationToken) || (cancellationToken.Type as IdentifierNameSyntax).Identifier.ValueText != "CancellationToken")
                {
                    return false;
                }
            }

            return true;
        }

        static readonly string[] handlerMethodNames = new[] { "Handle", "HandleAsync" };
        static readonly string[] timeoutMethodNames = new[] { "Timeout", "TimeoutAsync" };


        // {0} = Specific interface i.e. IHandleMessages<MyMessageClass>
        internal static readonly DiagnosticDescriptor MustImplementIHandleMessagesDiagnostic = new DiagnosticDescriptor(
            id: "NSB0002",
            title: "Must implement IHandleMessages<T>",
            messageFormat: "Must create a Handle method on classes implementing {0}.",
            category: "NServiceBus.Code",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "An NServiceBus message handler or saga must contain a message handler method when implementing IHandleMessages<T>. Use the code fixer to create the required method.");

        // {0} = Specific interface i.e. IAmStartedByMessages<MyMessageClass>
        internal static readonly DiagnosticDescriptor MustImplementIAmStartedByMessagesDiagnostic = new DiagnosticDescriptor(
            id: "NSB0003",
            title: "Must implement IAmStartedByMessages<T>",
            messageFormat: "Must create a Handle method on classes implementing {0}.",
            category: "NServiceBus.Code",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "An NServiceBus saga must contain a message handler method when implementing IHandleMessages<T>. Use the code fixer to create the required method.");

        // {0} = Specific interface i.e. IHandleTimeouts<MyTimeoutClass>
        internal static readonly DiagnosticDescriptor MustImplementIHandleTimeoutsDiagnostic = new DiagnosticDescriptor(
            id: "NSB0004",
            title: "Must implement IHandleTimeouts<T>",
            messageFormat: "Must create a Timeout method on classes implementing {0}.",
            category: "NServiceBus.Code",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "An NServiceBus saga must contain a Timeout handler method when implementing IHandleTimeouts<T>. Use the code fixer to create the required method.");

        // {0} = Message Type
        static readonly DiagnosticDescriptor tooManyHandleMethodsDiagnostic = new DiagnosticDescriptor(
            id: "NSB0005",
            title: "Too many handler methods",
            messageFormat: "Duplicate handler methods for the message type {0}.",
            category: "NServiceBus.Code",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: @"In an NServiceBus message handler or saga, only one method can handle each message type.");

    }
}
