using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.StorageClient.Protocol;
using System;
using System.Net;

namespace NServiceBus.Timeout.Hosting.Azure
{
    public static class LeaseBlobExtensions
    {
        public static string TryAcquireLease(this CloudBlob blob)
        {
            try { return blob.AcquireLease(); }
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

        public static string AcquireLease(this CloudBlob blob)
        {
            var creds = blob.ServiceClient.Credentials;
            var transformedUri = new Uri(creds.TransformUri(blob.Uri.AbsoluteUri));
            var req = BlobRequest.Lease(transformedUri, 90, LeaseAction.Acquire, null); 
            blob.ServiceClient.Credentials.SignRequest(req);
            using (var response = req.GetResponse()) {
                return response.Headers["x-ms-lease-id"];
            }
        }

        private static void DoLeaseOperation(CloudBlob blob, string leaseId, LeaseAction action)
        {
            var creds = blob.ServiceClient.Credentials;
            var transformedUri = new Uri(creds.TransformUri(blob.Uri.AbsoluteUri));
            var req = BlobRequest.Lease(transformedUri, 90, action, leaseId);
            creds.SignRequest(req);
            req.GetResponse().Close();
        }

        public static void ReleaseLease(this CloudBlob blob, string leaseId)
        {
            DoLeaseOperation(blob, leaseId, LeaseAction.Release);
        }

        public static bool TryRenewLease(this CloudBlob blob, string leaseId)
        {
            try { blob.RenewLease(leaseId); return true; }
            catch { return false; }
        }

        public static void RenewLease(this CloudBlob blob, string leaseId)
        {
            DoLeaseOperation(blob, leaseId, LeaseAction.Renew);
        }

        public static void BreakLease(this CloudBlob blob)
        {
            DoLeaseOperation(blob, null, LeaseAction.Break);
        }

        public static void SetMetadata(this CloudBlob blob, string leaseId)
        {
            var req = BlobRequest.SetMetadata(new Uri(blob.ServiceClient.Credentials.TransformUri(blob.Uri.AbsoluteUri)), 90, leaseId);
            foreach (string key in blob.Metadata.Keys)
            {
                req.Headers.Add("x-ms-meta-" + key, blob.Metadata[key]);
            }
            blob.ServiceClient.Credentials.SignRequest(req);
            req.GetResponse().Close();
        }
    }
}