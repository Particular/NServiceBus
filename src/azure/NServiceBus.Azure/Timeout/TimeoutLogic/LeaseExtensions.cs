using System;
using System.Net;

namespace NServiceBus.Azure
{
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Blob.Protocol;

    public static class LeaseBlobExtensions
    {
        public static string TryAcquireLease(this CloudBlockBlob blob)
        {
            try { return blob.AcquireLease(TimeSpan.FromSeconds(90), null); }
            catch (WebException e)
            {
                if (((HttpWebResponse)e.Response).StatusCode != HttpStatusCode.Conflict) // 409, already leased
                {
                    throw;
                }
                e.Response.Close();
                return null;
            }
        }

        public static bool TryRenewLease(this CloudBlockBlob blob, string leaseId)
        {
            try { blob.RenewLease(new AccessCondition()
                {
                    LeaseId = leaseId
                }); return true; }
            catch { return false; }
        }

    }
}