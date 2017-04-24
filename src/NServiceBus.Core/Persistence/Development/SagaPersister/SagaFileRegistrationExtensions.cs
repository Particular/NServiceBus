namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    static class SagaFileRegistrationExtensions
    {
        public static void RegisterSagaFile(this Dictionary<string, SagaStorageFile> sagaFiles, SagaStorageFile sagaStorageFile, Guid sagaId, Type sagaDataType)
        {
            sagaFiles[$"{sagaDataType.FullName}{sagaId}"] = sagaStorageFile;
        }
    }
}