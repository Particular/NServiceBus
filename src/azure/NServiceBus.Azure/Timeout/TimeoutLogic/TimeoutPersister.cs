using System.Globalization;
using System.Threading;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace NServiceBus.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Data.Services.Client;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Web.Script.Serialization;
    using Logging;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Support;
    using Timeout.Core;
    
    public class TimeoutPersister : IPersistTimeouts, IDetermineWhoCanSend
    {
        public List<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            List<Tuple<string, DateTime>> results = new List<Tuple<string, DateTime>>();
            try
            {
                var now = DateTime.UtcNow;
                var context = new ServiceContext(account.CreateCloudTableClient());
                TimeoutManagerDataEntity lastSuccessfullReadEntity;
                var lastSuccessfullRead = TryGetLastSuccessfullRead(context, out lastSuccessfullReadEntity)
                                              ? lastSuccessfullReadEntity.LastSuccessfullRead
                                              : default(DateTime?);

                IOrderedEnumerable<TimeoutDataEntity> result;

                if (lastSuccessfullRead.HasValue)
                {
                    result = (from c in context.TimeoutData
                              where c.PartitionKey.CompareTo(lastSuccessfullRead.Value.ToString(PartitionKeyScope)) >= 0
                              && c.PartitionKey.CompareTo(now.ToString(PartitionKeyScope)) <= 0
                                    && c.OwningTimeoutManager == Configure.EndpointName
                              select c).ToList().OrderBy(c => c.Time);
                }
                else
                {
                    result = (from c in context.TimeoutData
                              where c.OwningTimeoutManager == Configure.EndpointName
                              select c).ToList().OrderBy(c => c.Time);
                }

                var allTimeouts = result.ToList();
                if (allTimeouts.Count == 0)
                {
                    nextTimeToRunQuery = now.AddSeconds(1);
                    return results;
                }

                var pastTimeouts = allTimeouts.Where(c => c.Time > startSlice && c.Time <= now).ToList();
                var futureTimeouts = allTimeouts.Where(c => c.Time > now).ToList();

                if (lastSuccessfullReadEntity != null && lastSuccessfullRead.HasValue)
                {
                    var catchingUp = lastSuccessfullRead.Value.AddSeconds(CatchUpInterval);
                    lastSuccessfullRead = catchingUp > now ? now : catchingUp;
                    lastSuccessfullReadEntity.LastSuccessfullRead = lastSuccessfullRead.Value;
                }

                var future = futureTimeouts.FirstOrDefault();
                nextTimeToRunQuery = lastSuccessfullRead.HasValue ? lastSuccessfullRead.Value
                                         : (future != null ? future.Time : now.AddSeconds(1));
                
                results = pastTimeouts
                   .Where(c => !string.IsNullOrEmpty(c.RowKey))
                   .Select(c => new Tuple<String, DateTime>(c.RowKey, c.Time))
                   .Distinct()
                   .ToList();

                UpdateSuccesfullRead(context, lastSuccessfullReadEntity);
            }
            catch (DataServiceQueryException)
            {
                nextTimeToRunQuery = DateTime.UtcNow.AddMinutes(1);
                results = new List<Tuple<String, DateTime>>();
            }
            return results;
        }

        public void Add(TimeoutData timeout)
        {
            var context = new ServiceContext(account.CreateCloudTableClient());
            var hash = Hash(timeout);
            TimeoutDataEntity timeoutDataEntity;
            if (TryGetTimeoutData(context, hash, string.Empty, out timeoutDataEntity)) return;

            var stateAddress = Upload(timeout.State, hash);
            var headers = Serialize(timeout.Headers);

            if (!TryGetTimeoutData(context, timeout.Time.ToString(PartitionKeyScope), stateAddress, out timeoutDataEntity))
                context.AddObject(ServiceContext.TimeoutDataTableName,
                                      new TimeoutDataEntity(timeout.Time.ToString(PartitionKeyScope), stateAddress)
                                      {
                                          Destination = timeout.Destination.ToString(),
                                          SagaId = timeout.SagaId,
                                          StateAddress = stateAddress,
                                          Time = timeout.Time,
                                          CorrelationId = timeout.CorrelationId,
                                          OwningTimeoutManager = timeout.OwningTimeoutManager,
                                          Headers = headers
                                      });

            timeout.Id = stateAddress;

            if (timeout.SagaId != default(Guid) && !TryGetTimeoutData(context, timeout.SagaId.ToString(), stateAddress, out timeoutDataEntity))
                context.AddObject(ServiceContext.TimeoutDataTableName,
                                      new TimeoutDataEntity(timeout.SagaId.ToString(), stateAddress)
                                      {
                                          Destination = timeout.Destination.ToString(),
                                          SagaId = timeout.SagaId,
                                          StateAddress = stateAddress,
                                          Time = timeout.Time,
                                          CorrelationId = timeout.CorrelationId,
                                          OwningTimeoutManager = timeout.OwningTimeoutManager,
                                          Headers = headers
                                      });

            context.AddObject(ServiceContext.TimeoutDataTableName,
                                new TimeoutDataEntity(stateAddress, string.Empty)
                                {
                                    Destination = timeout.Destination.ToString(),
                                    SagaId = timeout.SagaId,
                                    StateAddress = stateAddress,
                                    Time = timeout.Time,
                                    CorrelationId = timeout.CorrelationId,
                                    OwningTimeoutManager = timeout.OwningTimeoutManager,
                                    Headers = headers
                                });

            context.SaveChanges();
        }

        public bool TryRemove(string timeoutId, out TimeoutData timeoutData)
        {
            timeoutData = null;

            var context = new ServiceContext(account.CreateCloudTableClient());
            try
            {
                TimeoutDataEntity timeoutDataEntity;
                if (!TryGetTimeoutData(context, timeoutId, string.Empty, out timeoutDataEntity))
                {
                    return false;
                }

                timeoutData = new TimeoutData
                {
                    Destination = Address.Parse(timeoutDataEntity.Destination),
                    SagaId = timeoutDataEntity.SagaId,
                    State = Download(timeoutDataEntity.StateAddress),
                    Time = timeoutDataEntity.Time,
                    CorrelationId = timeoutDataEntity.CorrelationId,
                    Id = timeoutDataEntity.RowKey,
                    OwningTimeoutManager = timeoutDataEntity.OwningTimeoutManager,
                    Headers = Deserialize(timeoutDataEntity.Headers)
                };

                TimeoutDataEntity timeoutDataEntityBySaga;
                if (TryGetTimeoutData(context, timeoutDataEntity.SagaId.ToString(), timeoutId, out timeoutDataEntityBySaga))
                {
                    context.DeleteObject(timeoutDataEntityBySaga);
                }

                TimeoutDataEntity timeoutDataEntityByTime;
                if (TryGetTimeoutData(context, timeoutDataEntity.Time.ToString(PartitionKeyScope), timeoutId, out timeoutDataEntityByTime))
                {
                    context.DeleteObject(timeoutDataEntityByTime);
                }

                RemoveState(timeoutDataEntity.StateAddress);

                context.DeleteObject(timeoutDataEntity);

                context.SaveChanges();
            }
            catch (Exception ex)
            {
                Logger.Debug(string.Format("Failed to clean up timeout {0}", timeoutId), ex);
            }

            return true;
        }

        public void RemoveTimeoutBy(Guid sagaId)
        {
            var context = new ServiceContext(account.CreateCloudTableClient());
            try
            {
                var results = (from c in context.TimeoutData
                               where c.PartitionKey == sagaId.ToString()
                               select c).ToList();

                foreach (var timeoutDataEntityBySaga in results)
                {
                    RemoveState(timeoutDataEntityBySaga.StateAddress);

                    TimeoutDataEntity timeoutDataEntityByTime;
                    if (TryGetTimeoutData(context, timeoutDataEntityBySaga.Time.ToString(PartitionKeyScope), timeoutDataEntityBySaga.RowKey, out timeoutDataEntityByTime))
                        context.DeleteObject(timeoutDataEntityByTime);

                    TimeoutDataEntity timeoutDataEntity;
                    if (TryGetTimeoutData(context, timeoutDataEntityBySaga.RowKey, string.Empty, out timeoutDataEntity))
                        context.DeleteObject(timeoutDataEntity);

                    context.DeleteObject(timeoutDataEntityBySaga);
                }
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                Logger.Debug(string.Format("Failed to clean up timeouts for saga {0}", sagaId), ex);
            }

        }

        private bool TryGetTimeoutData(ServiceContext context, string partitionkey, string rowkey, out TimeoutDataEntity result)
        {
            try
            {
                result = (from c in context.TimeoutData
                          where c.PartitionKey == partitionkey && c.RowKey == rowkey
                          select c).FirstOrDefault();
            }
            catch (Exception)
            {
                result = null;
            }

            return result != null;

        }

        public bool CanSend(TimeoutData data)
        {
            var context = new ServiceContext(account.CreateCloudTableClient());
            TimeoutDataEntity timeoutDataEntity;
            if (!TryGetTimeoutData(context, data.Id, string.Empty, out timeoutDataEntity)) return false;

            var leaseBlob = container.GetBlockBlobReference(timeoutDataEntity.StateAddress);

            using (var lease = new AutoRenewLease(leaseBlob))
            {
                return lease.HasLease;
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

        public int CatchUpInterval { get; set; }
        public string PartitionKeyScope { get; set; }

        private void Init(string connectionstring)
        {
            account = CloudStorageAccount.Parse(connectionstring);
            var context = new ServiceContext(account.CreateCloudTableClient());
            var tableClient = account.CreateCloudTableClient();
            var table = tableClient.GetTableReference(ServiceContext.TimeoutManagerDataTableName);
            table.CreateIfNotExists();
            table = tableClient.GetTableReference(ServiceContext.TimeoutDataTableName);
            table.CreateIfNotExists();
            container = account.CreateCloudBlobClient().GetContainerReference("timeoutstate");
            container.CreateIfNotExists();

            MigrateExistingTimeouts(context);
        }

        private void MigrateExistingTimeouts(ServiceContext context)
        {
            var existing = (from c in context.TimeoutData
                            where c.PartitionKey == "TimeoutData"
                            select c).ToList();

            foreach (var timeout in existing)
            {
                TimeoutDataEntity timeoutDataEntity;

                if (!TryGetTimeoutData(context, timeout.Time.ToString(PartitionKeyScope), timeout.RowKey, out timeoutDataEntity))
                    context.AddObject(ServiceContext.TimeoutDataTableName,
                                      new TimeoutDataEntity(timeout.Time.ToString(PartitionKeyScope), timeout.RowKey)
                                      {
                                          Destination = timeout.Destination,
                                          SagaId = timeout.SagaId,
                                          StateAddress = timeout.RowKey,
                                          Time = timeout.Time,
                                          CorrelationId = timeout.CorrelationId,
                                          OwningTimeoutManager = timeout.OwningTimeoutManager
                                      });

                if (!TryGetTimeoutData(context, timeout.SagaId.ToString(), timeout.RowKey, out timeoutDataEntity))
                    context.AddObject(ServiceContext.TimeoutDataTableName,
                                          new TimeoutDataEntity(timeout.SagaId.ToString(), timeout.RowKey)
                                          {
                                              Destination = timeout.Destination,
                                              SagaId = timeout.SagaId,
                                              StateAddress = timeout.RowKey,
                                              Time = timeout.Time,
                                              CorrelationId = timeout.CorrelationId,
                                              OwningTimeoutManager = timeout.OwningTimeoutManager
                                          });

                if (!TryGetTimeoutData(context, timeout.RowKey, string.Empty, out timeoutDataEntity))
                    context.AddObject(ServiceContext.TimeoutDataTableName,
                                      new TimeoutDataEntity(timeout.RowKey, string.Empty)
                                      {
                                          Destination = timeout.Destination,
                                          SagaId = timeout.SagaId,
                                          StateAddress = timeout.RowKey,
                                          Time = timeout.Time,
                                          CorrelationId = timeout.CorrelationId,
                                          OwningTimeoutManager = timeout.OwningTimeoutManager
                                      });

                context.DeleteObject(timeout);
                context.SaveChanges();
            }
        }

        private string Upload(byte[] state, string stateAddress)
        {
            var blob = container.GetBlockBlobReference(stateAddress);
            using (var stream = new MemoryStream(state))
            {
                blob.UploadFromStream(stream);
            }
            return stateAddress;
        }

        private byte[] Download(string stateAddress)
        {
            var blob = container.GetBlockBlobReference(stateAddress);
            using (var stream = new MemoryStream())
            {
                blob.DownloadToStream(stream);
                stream.Position = 0;

                var buffer = new byte[16*1024];
                using (var ms = new MemoryStream())
                {
                    int read;
                    while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        ms.Write(buffer, 0, read);
                    }
                    return ms.ToArray();
                }
            }
        }

        private string Serialize(Dictionary<string, string> headers)
        {
            var serializer = new JavaScriptSerializer();
            return serializer.Serialize(headers);
        }

        private Dictionary<string, string> Deserialize(string state)
        {
            if (string.IsNullOrEmpty(state)) return new Dictionary<string, string>();

            var serializer = new JavaScriptSerializer();
            return serializer.Deserialize<Dictionary<string, string>>(state);
        }

        private void RemoveState(string stateAddress)
        {
            var blob = container.GetBlockBlobReference(stateAddress);
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

        private string GetUniqueEndpointName()
        {
            var identifier = RoleEnvironment.IsAvailable ? RoleEnvironment.CurrentRoleInstance.Id : RuntimeEnvironment.MachineName;

            return Configure.EndpointName + "_" + identifier;
        }

        private bool TryGetLastSuccessfullRead(ServiceContext context, out TimeoutManagerDataEntity lastSuccessfullReadEntity)
        {
            try
            {
                lastSuccessfullReadEntity = (from m in context.TimeoutManagerData
                                             where m.PartitionKey == GetUniqueEndpointName()
                                             select m).FirstOrDefault();
            }
            catch
            {

                lastSuccessfullReadEntity = null;
            }


            return lastSuccessfullReadEntity != null;
        }

        private void UpdateSuccesfullRead(ServiceContext context, TimeoutManagerDataEntity read)
        {
            try
            {
                if (read == null)
                {
                    read = new TimeoutManagerDataEntity(GetUniqueEndpointName(), string.Empty){LastSuccessfullRead = DateTime.UtcNow};

                    context.AddObject(ServiceContext.TimeoutManagerDataTableName, read);
                }
                else
                {
                    context.Detach(read);
                    context.AttachTo(ServiceContext.TimeoutManagerDataTableName, read, "*");
                    context.UpdateObject(read);
                }
                context.SaveChangesWithRetries(SaveChangesOptions.ReplaceOnUpdate);
            }
            catch (DataServiceRequestException ex) // handle concurrency issues
            {
                var response = ex.Response.FirstOrDefault();
                //Concurrency Exception - PreCondition Failed or Entity Already Exists
                if (response != null && (response.StatusCode == 412 || response.StatusCode == 409))
                {
                    return; 
                    // I assume we can ignore this condition? 
                    // Time between read and update is very small, meaning that another instance has sent 
                    // the timeout messages that this node intended to send and if not we will resend 
                    // anything after the other node's last read value anyway on next request.
                }

                throw;
            }

        }

        private string connectionString;
        private CloudStorageAccount account;
        private CloudBlobContainer container;

        static readonly ILog Logger = LogManager.GetLogger(typeof(TimeoutPersister));
    }
}
