namespace NServiceBus.Core.Analyzer
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    class SagaDetails
    {
        public INamedTypeSymbol SagaType { get; }
        public INamedTypeSymbol DataType { get; }
        public MethodDeclarationSyntax ConfigureHowToFindMethod { get; }
        public ImmutableArray<SagaHandlerDeclaration> StartedBy { get; }
        public ImmutableArray<SagaHandlerDeclaration> Handles { get; }
        public ImmutableArray<SagaHandlerDeclaration> Timeouts { get; }
        public List<SagaMessageMapping> MessageMappings { get; }
        public ImmutableHashSet<ITypeSymbol> MessageTypesHandled { get; }

        internal SagaDetails(INamedTypeSymbol sagaType, INamedTypeSymbol dataType, MethodDeclarationSyntax configureHowToFindMethod, ImmutableArray<SagaHandlerDeclaration> handles, ImmutableArray<SagaHandlerDeclaration> startedBy, ImmutableArray<SagaHandlerDeclaration> timeouts)
        {
            SagaType = sagaType;
            DataType = dataType;
            ConfigureHowToFindMethod = configureHowToFindMethod;
            Handles = handles;
            StartedBy = startedBy;
            Timeouts = timeouts;
            MessageMappings = new List<SagaMessageMapping>();

            MessageTypesHandled = StartedBy.Concat(Handles).Concat(Timeouts)
                .Select(declaration => declaration.MessageType as ITypeSymbol)
                .Distinct() // Could have IHandleMessages and IHandleTimeouts on same type!
                .ToImmutableHashSet();
        }

        public ParameterSyntax MapperParameterSyntax { get; set; }
    }
}
