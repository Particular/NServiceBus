using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using log4net;

namespace NServiceBus.Timeout.Hosting.Azure
{
    using Core;

    public class TimeoutPersister : IPersistTimeouts, IDetermineWhoCanSend
    {
        public List<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            var context = new ServiceContext(account.TableEndpoint.ToString(), account.Credentials);
            TimeoutManagerDataEntity lastSuccessfullReadEntity;
            var lastSuccessfullRead = TryGetLastSuccessfullRead(context, out lastSuccessfullReadEntity)
                                          ? lastSuccessfullReadEntity.LastSuccessfullRead
                                          : DateTime.UtcNow;
            

            var result = (from c in context.TimeoutData
                          where c.PartitionKey == lastSuccessfullRead.ToString("yyyMMddHH")
                          && c.OwningTimeoutManager == Configure.EndpointName
                          select c).ToList().OrderBy(c => c.Time);

            var allTimeouts = result.ToList();
            var pastTimeouts = allTimeouts.Where(c => c.Time <= DateTime.UtcNow).ToList();
            var futureTimeouts = allTimeouts.Where(c => c.Time > DateTime.UtcNow).ToList();

            nextTimeToRunQuery = futureTimeouts.Count == 0 ? lastSuccessfullRead.AddMinutes(1) : futureTimeouts.First().Time;

            var results = pastTimeouts
                .Select(c => new Tuple<String, DateTime>(c.RowKey, c.Time))
                .ToList();

            UpdateSuccesfullRead(context, lastSuccessfullReadEntity);

            return results;
        }

        public void Add(TimeoutData timeout)
        {
            var context = new ServiceContext(account.TableEndpoint.ToString(), account.Credentials);
            var hash = Hash(timeout);
            TimeoutDataEntity timeoutDataEntity;
            if (TryGetTimeoutData(context, hash, string.Empty, out timeoutDataEntity)) return;

            var stateAddress = Upload(timeout.State, hash);
            var headers = Serialize(timeout.Headers);

            if (!TryGetTimeoutData(context, timeout.Time.ToString("yyyMMddHH"), stateAddress, out timeoutDataEntity))
                context.AddObject(ServiceContext.TimeoutDataEntityTableName,
                                      new TimeoutDataEntity(timeout.Time.ToString("yyyMMddHH"), stateAddress)
                                          {
                                              Destination = timeout.Destination.ToString(),
                                              SagaId = timeout.SagaId,
                                              StateAddress = stateAddress,
                                              Time = timeout.Time,
                                              CorrelationId = timeout.CorrelationId,
                                              OwningTimeoutManager = timeout.OwningTimeoutManager,
                                              Headers = headers
                                          });


            if (timeout.SagaId != default(Guid) && !TryGetTimeoutData(context, timeout.SagaId.ToString(), stateAddress, out timeoutDataEntity))
                context.AddObject(ServiceContext.TimeoutDataEntityTableName,
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

            context.AddObject(ServiceContext.TimeoutDataEntityTableName,
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

            var context = new ServiceContext(account.TableEndpoint.ToString(), account.Credentials);
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
                if (TryGetTimeoutData(context, timeoutDataEntity.Time.ToString("yyyMMddHH"), timeoutId, out timeoutDataEntityByTime))
                {
                    context.DeleteObject(timeoutDataEntityByTime);
                }

                RemoveState(timeoutDataEntity.StateAddress);
                
                context.DeleteObject(timeoutDataEntity);

                context.SaveChanges();
            }
            catch
            {
                // make sure to add logging here
            }

            return true;
        }

        public void RemoveTimeoutBy(Guid sagaId)
        {
            var context = new ServiceContext(account.TableEndpoint.ToString(), account.Credentials);
            try
            {
                var results = (from c in context.TimeoutData
                               where c.PartitionKey == sagaId.ToString()
                               select c).ToList();

                foreach (var timeoutDataEntityBySaga in results)
                {
                    RemoveState(timeoutDataEntityBySaga.StateAddress);

                    TimeoutDataEntity timeoutDataEntityByTime;
                    if (TryGetTimeoutData(context, timeoutDataEntityBySaga.Time.ToString("yyyMMddHH"), timeoutDataEntityBySaga.RowKey, out timeoutDataEntityByTime))
                        context.DeleteObject(timeoutDataEntityByTime);

                    TimeoutDataEntity timeoutDataEntity;
                    if (TryGetTimeoutData(context, timeoutDataEntityBySaga.RowKey, string.Empty, out timeoutDataEntity))
                        context.DeleteObject(timeoutDataEntity);

                    context.DeleteObject(timeoutDataEntityBySaga);
                }
                context.SaveChanges();
            }
            catch
            {
                // make sure to add logging here
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
            var context = new ServiceContext(account.TableEndpoint.ToString(), account.Credentials);
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

        private void Init(string connectionstring)
        {
            account = CloudStorageAccount.Parse(connectionstring);
            var context = new ServiceContext(account.TableEndpoint.ToString(), account.Credentials);
            var tableClient = account.CreateCloudTableClient();
            tableClient.CreateTableIfNotExist(ServiceContext.TimeoutManagerDataEntityTableName);
            tableClient.CreateTableIfNotExist(ServiceContext.TimeoutDataEntityTableName);
            container = account.CreateCloudBlobClient().GetContainerReference("timeoutstate");
            container.CreateIfNotExist();

            MigrateExistingTimeouts(context);
        }

        private void MigrateExistingTimeouts(ServiceContext context)
        {
            var existing = (from c in context.TimeoutData
                          where c.PartitionKey == "TimeoutData"
                          select c).ToList();

            foreach(var timeout in existing)
            {
                TimeoutDataEntity timeoutDataEntity;

                if (!TryGetTimeoutData(context, timeout.Time.ToString("yyyMMddHH"), timeout.RowKey, out timeoutDataEntity))
                    context.AddObject(ServiceContext.TimeoutDataEntityTableName,
                                      new TimeoutDataEntity(timeout.Time.ToString("yyyMMddHH"), timeout.RowKey)
                                          {
                                              Destination = timeout.Destination,
                                              SagaId = timeout.SagaId,
                                              StateAddress = timeout.RowKey,
                                              Time = timeout.Time,
                                              CorrelationId = timeout.CorrelationId,
                                              OwningTimeoutManager = timeout.OwningTimeoutManager
                                          });

                if (!TryGetTimeoutData(context, timeout.SagaId.ToString(), timeout.RowKey, out timeoutDataEntity))
                    context.AddObject(ServiceContext.TimeoutDataEntityTableName,
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
                    context.AddObject(ServiceContext.TimeoutDataEntityTableName,
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
            blob.UploadByteArray(state);
            return stateAddress;
        }


        private byte[] Download(string stateAddress)
        {
            var blob = container.GetBlockBlobReference(stateAddress);
            return blob.DownloadByteArray();
        }

        private string Serialize(Dictionary<string, string> headers )
        {
            var serializer = new JavaScriptSerializer();
            return serializer.Serialize(headers);
        }

        private Dictionary<string, string> Deserialize(string state)
        {
            if(string.IsNullOrEmpty(state)) return new Dictionary<string, string>();

            var serializer = new JavaScriptSerializer();
            return serializer.Deserialize<Dictionary<string, string>>(state);
        }

        private void RemoveState(string stateAddress)
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

        private bool TryGetLastSuccessfullRead(ServiceContext context, out TimeoutManagerDataEntity lastSuccessfullReadEntity)
        {
            try
            {
                lastSuccessfullReadEntity = (from m in context.TimeoutManagerData
                                                 where m.PartitionKey == Configure.EndpointName
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
            if (read == null)
            {
                read = new TimeoutManagerDataEntity(Configure.EndpointName, string.Empty){
                               LastSuccessfullRead = DateTime.UtcNow
                           };

                context.AddObject(ServiceContext.TimeoutManagerDataEntityTableName, read);
            }
            else
            {
                read.LastSuccessfullRead = DateTime.UtcNow;
                context.UpdateObject(read);
            }

            context.SaveChanges(SaveChangesOptions.ReplaceOnUpdate);
        }

        private string connectionString;
        private CloudStorageAccount account;
        private CloudBlobContainer container;

        static readonly ILog Logger = LogManager.GetLogger("AzureTimeoutPersistence");

    }

}
