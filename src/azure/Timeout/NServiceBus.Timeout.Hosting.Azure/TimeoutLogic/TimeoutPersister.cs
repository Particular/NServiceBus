using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Newtonsoft.Json;
using Timeout.MessageHandlers;

namespace NServiceBus.Timeout.Hosting.Azure
{
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
                        Time = c.Time
                    });
        }


        void IPersistTimeouts.Add(TimeoutData timeout)
        {
            var stateAddress = Serialize(timeout.State, Hash(timeout));

            context.AttachTo(ServiceContext.TimeoutDataEntityTableName,
                                  new TimeoutDataEntity("TimeoutData", stateAddress)
                                      {
                                          Destination = timeout.Destination.ToString(),
                                          SagaId = timeout.SagaId,
                                          StateAddress = stateAddress,
                                          Time = timeout.Time
                                      });
            context.SaveChanges(SaveChangesOptions.ReplaceOnUpdate);
        }


        void IPersistTimeouts.Remove(Guid sagaId)
        {
            try
            {
                var results = (from c in context.TimeoutData
                               where c.SagaId == sagaId
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
            var hash = Hash(data);
            var result = (from c in context.TimeoutData
                          where c.RowKey == hash
                          select c).SingleOrDefault();

            if (result == null) return false;
            
            var leaseBlob = container.GetBlockBlobReference(result.StateAddress);

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
            var account = CloudStorageAccount.Parse(connectionstring);
            context = new ServiceContext(account.TableEndpoint.ToString(), account.Credentials);
            account.CreateCloudTableClient().CreateTableIfNotExist(ServiceContext.TimeoutDataEntityTableName);
            container = account.CreateCloudBlobClient().GetContainerReference("timeoutstate");
            container.CreateIfNotExist();
        }

        private string Serialize(object state, string hash)
        {
            using (var stream = new MemoryStream())
            {
                 var streamWriter = new StreamWriter(stream, Encoding.UTF8);
                 var writer = new JsonTextWriter(streamWriter) { Formatting = Formatting.Indented };
                 var serializer = CreateJsonSerializer();
                 serializer.Serialize(writer, state);

                var blob = container.GetBlockBlobReference(hash);
                blob.UploadFromStream(stream);
            }
            return hash;
        }

        private object Deserialize(string stateAddress)
        {
            var blob = container.GetBlockBlobReference(stateAddress);
            using (var stream = new MemoryStream())
            {
                blob.DownloadToStream(stream);
                stream.Seek(0, SeekOrigin.Begin);

                var streamReader = new StreamReader(stream, Encoding.UTF8);
                var reader = new JsonTextReader(streamReader);
                var serializer = CreateJsonSerializer();
                return serializer.Deserialize(reader);
            }
        }

        private void RemoveSerializedState(string stateAddress)
        {
            var blob = container.GetBlobReference(stateAddress);
            blob.DeleteIfExists();
        }

        private JsonSerializer CreateJsonSerializer()
        {
            var serializerSettings = new JsonSerializerSettings
            {
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
                TypeNameHandling = TypeNameHandling.Objects
            };
            
            return JsonSerializer.Create(serializerSettings);
        }

        private string Hash(TimeoutData timeout)
        {
            var s = timeout.SagaId + timeout.Destination + timeout.Time.Ticks;
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
