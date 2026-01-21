namespace NServiceBus.Core.Analyzer.Sagas;

using Microsoft.CodeAnalysis;
using static NServiceBus.Core.Analyzer.Sagas.Sagas;

public partial class AddSagaGenerator
{
    internal class Emitter(SourceProductionContext sourceProductionContext)
    {
        public void Emit(SagaSpecs handlerSpecs) => Emit(sourceProductionContext, handlerSpecs);

#pragma warning disable IDE0060
        static void Emit(SourceProductionContext context, SagaSpecs sagaSpecs)
#pragma warning restore IDE0060
        {
            var sagas = sagaSpecs.Sagas;
            if (sagas.Count == 0)
            {
                return;
            }
        }
    }
}