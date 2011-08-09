using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using NServiceBus;
using Newtonsoft.Json;

namespace Timeout.MessageHandlers
{
    public class TimeoutPersister : IPersistTimeouts
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
            string stateAddress = Serialize(timeout.State);

            context.AddObject(ServiceContext.TimeoutDataEntityTableName,
                                  new TimeoutDataEntity(stateAddress, "TimeoutData")
                                      {
                                          Destination = timeout.Destination.ToString(),
                                          SagaId = timeout.SagaId,
                                          StateAddress = stateAddress,
                                          Time = timeout.Time
                                      });
            context.SaveChanges();
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

        private string Serialize(object state)
        {
            string stateAddress;
            using (var stream = new MemoryStream())
            {
                 var streamWriter = new StreamWriter(stream, Encoding.UTF8);
                 var writer = new JsonTextWriter(streamWriter) { Formatting = Formatting.Indented };
                 var serializer = CreateJsonSerializer();
                 serializer.Serialize(writer, state);

                stateAddress = Guid.NewGuid().ToString();
                var blob = container.GetBlockBlobReference(stateAddress);
                blob.UploadFromStream(stream);
            }
            return stateAddress;
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
                TypeNameHandling = TypeNameHandling.All
            };
            
            return JsonSerializer.Create(serializerSettings);
        } 
        
        private string connectionString;
        private ServiceContext context;
        private CloudBlobContainer container;

    }
}
