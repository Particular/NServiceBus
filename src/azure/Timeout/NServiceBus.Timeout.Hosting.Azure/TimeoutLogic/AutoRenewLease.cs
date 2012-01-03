using Microsoft.WindowsAzure.StorageClient;
using System;
using System.Threading;
using System.Net;

namespace NServiceBus.Timeout.Hosting.Azure
{
    public class AutoRenewLease : IDisposable
    {
        public bool HasLease { get { return leaseId != null; } }

        private readonly CloudBlob blob;
        private readonly string leaseId;
        private Thread renewalThread;
        private bool disposed;

        public AutoRenewLease(CloudBlob blob)
        {
            this.blob = blob;
            blob.Container.CreateIfNotExist();
            try
            {
                blob.UploadByteArray(new byte[0], new BlobRequestOptions { AccessCondition = AccessCondition.IfNoneMatch("*") });
            }
            catch (StorageClientException e)
            {
                if (e.ErrorCode != StorageErrorCode.BlobAlreadyExists
                    && e.StatusCode != HttpStatusCode.PreconditionFailed) // 412 from trying to modify a blob that's leased
                {
                    throw;
                }
            }
            leaseId = blob.TryAcquireLease();
            if (HasLease)
            {
                renewalThread = new Thread(() =>
                {
                    Thread.Sleep(TimeSpan.FromSeconds(40));
                    blob.RenewLease(leaseId);
                });
                renewalThread.Start();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (renewalThread != null)
                    {
                        renewalThread.Abort();
                        blob.ReleaseLease(leaseId);
                        renewalThread = null;
                    }
                }
                disposed = true;
            }
        }

        ~AutoRenewLease()
        {
            Dispose(false);
        }
    }
}