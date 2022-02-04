namespace NServiceBus.Core.Analyzer
{
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    class SagaHandlerDeclaration
    {
        public BaseTypeSyntax Syntax { get; }
        public TypeSyntax MessageTypeSyntax { get; }
        public INamedTypeSymbol InterfaceType { get; }
        public INamedTypeSymbol MessageType { get; }

        public SagaHandlerDeclaration(BaseTypeSyntax syntax, INamedTypeSymbol interfaceType)
        {
            Syntax = syntax;
            MessageTypeSyntax = (syntax.Type as GenericNameSyntax)?.TypeArgumentList?.Arguments.FirstOrDefault();

            InterfaceType = interfaceType;
            MessageType = interfaceType.TypeArguments.FirstOrDefault() as INamedTypeSymbol;
        }

        public override string ToString() => Syntax.ToFullString();
    }
}
