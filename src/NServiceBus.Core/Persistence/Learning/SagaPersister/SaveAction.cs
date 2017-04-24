namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Sagas;

    class SaveAction : StorageAction
    {
        SagaCorrelationProperty correlationProperty;

        public SaveAction(SagaCorrelationProperty correlationProperty, IContainSagaData sagaData, Dictionary<string, SagaStorageFile> sagaFiles, SagaManifestCollection sagaManifests) : base(sagaData, sagaFiles, sagaManifests)
        {
            this.correlationProperty = correlationProperty;
        }

        public override async Task Execute()
        {
            var sagaId = sagaData.Id;
            var sagaManifest = sagaManifests.GetForEntityType(sagaData.GetType());

            try
            {
                var sagaFile = SagaStorageFile.Create(sagaId, sagaManifest);
                sagaFiles.RegisterSagaFile(sagaFile, sagaId, sagaManifest.SagaEntityType);

                await sagaFile.Write(sagaData)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when(ex is ConcurrencyException || ex is IOException)
            {
                if (correlationProperty == SagaCorrelationProperty.None)
                {
                    throw new Exception("A saga with this identifier already exists. This should never happened as saga identifier are meant to be unique.");
                }

                throw new Exception($"The saga with the correlation id 'Name: {correlationProperty.Name} Value: {correlationProperty.Value}' already exists.");
            }
        }
    }
}