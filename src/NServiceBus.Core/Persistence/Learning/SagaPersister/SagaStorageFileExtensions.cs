#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;

static class SagaStorageFileExtensions
{
    extension(Dictionary<string, SagaStorageFile> sagaFiles)
    {
        public void RegisterSagaFile(SagaStorageFile sagaStorageFile, Guid sagaId, Type sagaDataType) => sagaFiles[$"{sagaDataType.FullName}{sagaId}"] = sagaStorageFile;
    }
}