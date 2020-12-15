namespace NServiceBus.Core.Analyzer
{
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
            MustImplementDiagnostic,
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
                    if (BaseTypeIsHandlerSignature(baseTypeSyntax, out var messageIdentifier))
                    {
                        if (!HasImplementationDefined(context, classDeclaration, messageIdentifier))
                        {
                            var location = baseTypeSyntax.GetLocation();
                            var properties = new Dictionary<string, string> { { "MessageType", messageIdentifier } }.ToImmutableDictionary();
                            var diagnostic = Diagnostic.Create(MustImplementDiagnostic, location, properties);
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }

        private static bool BaseTypeIsHandlerSignature(BaseTypeSyntax baseTypeSyntax, out string messageIdentifier)
        {
            messageIdentifier = null;

            var namePart = baseTypeSyntax.GetFirstToken();
            if (namePart == null)
            {
                return false;
            }

            if (namePart.Text != "IHandleMessages" && namePart.Text != "IAmStartedByMessages")
            {
                return false;
            }

            var lessThanToken = namePart.GetNextToken();
            if (lessThanToken == null || lessThanToken.Text != "<")
            {
                return false;
            }

            var tClassToken = lessThanToken.GetNextToken();
            if (tClassToken == null)
            {
                return false;
            }
            messageIdentifier = tClassToken.Text;

            var gtToken = tClassToken.GetNextToken();
            if (gtToken == null || gtToken.Text != ">")
            {
                return false;
            }

            return true;
        }

        private static bool HasImplementationDefined(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax classDeclaration, string messageIdentifier)
        {
            bool foundHandler = false;
            SyntaxToken firstHandler = default(SyntaxToken);
            List<SyntaxToken> additionalHandlers = null;

            foreach (var member in classDeclaration.Members)
            {
                if (member is MethodDeclarationSyntax methodDeclaration)
                {
                    if (IsMethodAHandleMethod(methodDeclaration, messageIdentifier))
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
                var diagnostic = Diagnostic.Create(tooManyHandleMethodsDiagnostic, firstHandler.GetLocation(), additionalLocations);
                context.ReportDiagnostic(diagnostic);
            }

            return foundHandler;
        }

        static bool IsMethodAHandleMethod(MethodDeclarationSyntax methodDeclaration, string messageIdentifier)
        {
            var methodName = methodDeclaration.Identifier.Text;

            if (methodName != "Handle" && methodName != "HandleAsync")
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

        internal static readonly DiagnosticDescriptor MustImplementDiagnostic = new DiagnosticDescriptor(
            "NSB0002",
            "Must implement Handle method",
            "Must create a Handle or HandleAsync method on classes implementing IHandleMessages<T>.",
            "NServiceBus.Code",
            DiagnosticSeverity.Error,
            true,
            "A class implementing the marker interface IHandleMessages<T> or IAmStartedByMessages<T> must include either a Handle or HandleAsync method.");

        static readonly DiagnosticDescriptor tooManyHandleMethodsDiagnostic = new DiagnosticDescriptor(
            "NSB0003",
            "Too many Handle/HandleAsync methods",
            "Duplicate Handle/HandleAsync methods for the same message type.",
            "NServiceBus.Code",
            DiagnosticSeverity.Error,
            true,
            "A class implementing the marker interface IHandleMessages<T> or IAmStartedByMessages<T> can only include one Handle/HandleAsync method for each message type.");

    }
}
