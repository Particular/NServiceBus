using Microsoft.WindowsAzure.StorageClient;

namespace NServiceBus.Timeout.Hosting.Azure
{
    public static class ExistenceExtensions
    {
        public static bool Exists(this CloudBlob blob)
        {
            try
            {
                blob.FetchAttributes();
                return true;
            }
            catch (StorageClientException e)
            {
                if (e.ErrorCode == StorageErrorCode.ResourceNotFound)
                {
                    return false;
                }
                throw;
            }
        }
    }
}