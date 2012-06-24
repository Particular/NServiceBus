using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace NServiceBus.Timeout.Hosting.Azure
{
    using Core;

    public class TimeoutPersister : IPersistTimeouts, IDetermineWhoCanSend
    {
        IEnumerable<TimeoutData> IPersistTimeouts.GetAll()
        {
            var result = (from c in context.TimeoutData
                          where c.PartitionKey == "TimeoutData"
                          select c).ToList();

            return result.Select(c => new TimeoutData
                    {
                        Destination = Address.Parse(c.Destination),
                        SagaId = c.SagaId,
                        State = Deserialize(c.StateAddress),
                        Time = c.Time,
                        CorrelationId = c.CorrelationId,
                        Id = c.RowKey
                    });
        }

        void IPersistTimeouts.Add(TimeoutData timeout)
        {
            var hash = Hash(timeout);
            TimeoutDataEntity timeoutDataEntity;
            if (TryGetTimeoutData(hash, out timeoutDataEntity)) return;

            var stateAddress = Serialize(timeout.State, hash);
            timeout.Id = stateAddress;

            context.AddObject(ServiceContext.TimeoutDataEntityTableName,
                                  new TimeoutDataEntity("TimeoutData", stateAddress)
                                      {
                                          Destination = timeout.Destination.ToString(),
                                          SagaId = timeout.SagaId,
                                          StateAddress = stateAddress,
                                          Time = timeout.Time,
                                          CorrelationId = timeout.CorrelationId
                                      });

            context.SaveChanges();
        }

        public void Remove(string timeoutId)
        {
            TimeoutDataEntity timeoutDataEntity;
            if (!TryGetTimeoutData(timeoutId, out timeoutDataEntity)) return;

            RemoveSerializedState(timeoutDataEntity.StateAddress);
            context.DeleteObject(timeoutDataEntity);

            context.SaveChanges();
        }

        public void ClearTimeoutsFor(Guid sagaId)
        {
            try
            {
                var results = (from c in context.TimeoutData
                               where c.PartitionKey == "TimeoutData" && c.SagaId == sagaId
                               select c).ToList();

                foreach (var timeoutDataEntity in results)
                {
                    RemoveSerializedState(timeoutDataEntity.StateAddress);
                    context.DeleteObject(timeoutDataEntity);
                }
                context.SaveChanges();
            }
            catch
            {
                // make sure to add logging here
            }

        }

        public bool CanSend(TimeoutData data)
        {
            TimeoutDataEntity timeoutDataEntity;
            if (!TryGetTimeoutData(data.Id, out timeoutDataEntity)) return false;

            var leaseBlob = container.GetBlockBlobReference(timeoutDataEntity.StateAddress);

            using (var lease = new AutoRenewLease(leaseBlob))
            {
                return lease.HasLease;
            }
        }

        private bool TryGetTimeoutData(string hash, out TimeoutDataEntity result)
        {
            try
            {
                result = (from c in context.TimeoutData
                          where c.PartitionKey == "TimeoutData" && c.RowKey == hash
                          select c).SingleOrDefault();

                return result != null;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        public string ConnectionString
        {
            get
            {
                return connectionString;
            }
            set
            {
                connectionString = value;
                Init(connectionString);
            }
        }

        private void Init(string connectionstring)
        {
            var account = CloudStorageAccount.Parse(connectionstring);
            context = new ServiceContext(account.TableEndpoint.ToString(), account.Credentials);
            account.CreateCloudTableClient().CreateTableIfNotExist(ServiceContext.TimeoutDataEntityTableName);
            container = account.CreateCloudBlobClient().GetContainerReference("timeoutstate");
            container.CreateIfNotExist();
        }

        private string Serialize(byte[] state, string hash)
        {
            var blob = container.GetBlockBlobReference(hash);
            blob.UploadByteArray(state);
            return hash;
        }

        private byte[] Deserialize(string stateAddress)
        {
            var blob = container.GetBlockBlobReference(stateAddress);
            return blob.DownloadByteArray();
        }

        private void RemoveSerializedState(string stateAddress)
        {
            var blob = container.GetBlobReference(stateAddress);
            blob.DeleteIfExists();
        }

        private static string Hash(TimeoutData timeout)
        {
            var s = timeout.SagaId + timeout.Destination.ToString() + timeout.Time.Ticks;
            var sha1 = SHA1.Create();
            var bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(s));

            var hash = new StringBuilder();
            for (var i = 0; i < bytes.Length; i++)
            {
                hash.Append(bytes[i].ToString("X2"));
            }
            return hash.ToString();
        }

        private string connectionString;
        private ServiceContext context;
        private CloudBlobContainer container;

    }
}
