namespace NServiceBus.Persistence.Raven
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using global::Raven.Abstractions.Extensions;
    using global::Raven.Client.Connection;
    using global::Raven.Client.Document;
    using global::Raven.Json.Linq;
    using Installation;
    using Installation.Environments;
    using Logging;

    /// <summary>
    /// Add the identity to the Raven users group 
    /// </summary>
    public class RavenUserInstaller : INeedToInstallSomething<Windows>
    {
        static readonly ILog logger = LogManager.GetLogger(typeof(RavenUserInstaller));

        public StoreAccessor StoreAccessor { get; set; }

        internal static bool RunInstaller { get; set; }

        public void Install(string identity)
        {
            if (!RunInstaller)
            {
                return;
            }

            var store = StoreAccessor.Store as DocumentStore;

            if (store == null)
            {
                return;
            }

            try
            {
                AddUserToDatabase(identity, store);
            }
            catch (Exception exception)
            {
                logger.Warn("Failed to add user to raven. Processing will continue", exception);
            }
        }

        internal static void AddUserToDatabase(string identity, DocumentStore documentStore)
        {
            var database = documentStore.DefaultDatabase ?? "<system>";

            logger.InfoFormat(string.Format("Adding user '{0}' to raven. Instance:'{1}', Database:'{2}'.", identity, documentStore.Url, database));

            var systemCommands = documentStore
                .DatabaseCommands
                .ForSystemDatabase();
            var existing = systemCommands.Get("Raven/Authorization/WindowsSettings");

            WindowsAuthDocument windowsAuthDocument;
            if (existing == null)
            {
                windowsAuthDocument = new WindowsAuthDocument();
            }
            else
            {
                windowsAuthDocument = existing
                    .DataAsJson
                    .JsonDeserialization<WindowsAuthDocument>();
            }
            AddOrUpdateAuthUser(windowsAuthDocument, identity, database);

            var ravenJObject = RavenJObject.FromObject(windowsAuthDocument);

            InvokePut(systemCommands, ravenJObject);
        }

        static void InvokePut(IDatabaseCommands systemCommands, RavenJObject ravenJObject)
        {
            //in the move to raven 2.5 a breaking change was made to IDatabaseCommands.Put
            //in 2.0 
            //PutResult Put(string key, Guid? etag, RavenJObject document, RavenJObject metadata);
            //in 2.5  
            //PutResult Put(string key, Etag etag, RavenJObject document, RavenJObject metadata);
            var databaseCommandsType = typeof(IDatabaseCommands);
            var putMethod = databaseCommandsType.GetMethod("Put", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(string), typeof(Guid?), typeof(RavenJObject), typeof(RavenJObject) }, null);
            if (putMethod == null)
            {
                var ravenAbstractionAssembly = typeof(RavenJObject).Assembly;
                //Cant use Etag in a strong typed way because the namespace of Etag changed
                var etagType = ravenAbstractionAssembly.GetType("Raven.Abstractions.Data.Etag");
                if (etagType == null)
                {
                    var message = string.Format("Could not find `Raven.Abstractions.Data.Etag` in `{0}` there has possibly been a breaking change in RavenDB.", ravenAbstractionAssembly.FullName);
                    throw new Exception(message);
                }
                putMethod = databaseCommandsType.GetMethod("Put", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(string), etagType, typeof(RavenJObject), typeof(RavenJObject) }, null);
                if (putMethod == null)
                {
                    throw new Exception("Could not extract `IDatabaseCommands.Put` from the current version of RavenDB.");
                }
            }
            try
            {
                putMethod.Invoke(systemCommands, new object[] { "Raven/Authorization/WindowsSettings", null, ravenJObject, new RavenJObject() });
            }
            catch (TargetInvocationException exception)
            {
                //need to catch OperationVetoedException here but cant do it in a strong typed way since the namespace of OperationVetoedException changed in 2.5  but cant because in 
                if (exception.InnerException.Message.Contains("Cannot setup Windows Authentication without a valid commercial license."))
                {
                    throw new Exception("RavenDB requires a Commercial license to configure windows authentication. Please either install your RavenDB license or contact support@particular.net if you need a copy of the RavenDB license.");
                }
                throw;
            }
        }

        static void AddOrUpdateAuthUser(WindowsAuthDocument windowsAuthDocument, string identity, string tenantId)
        {
            var windowsAuthForUser = windowsAuthDocument
                .RequiredUsers
                .FirstOrDefault(x => x.Name == identity);
            if (windowsAuthForUser == null)
            {
                windowsAuthForUser = new WindowsAuthData
                                     {
                                         Name = identity
                                     };
                windowsAuthDocument.RequiredUsers.Add(windowsAuthForUser);
            }
            windowsAuthForUser.Enabled = true;

            AddOrUpdateDataAccess(windowsAuthForUser, tenantId);
        }

        static void AddOrUpdateDataAccess(WindowsAuthData windowsAuthForUser, string tenantId)
        {
            var dataAccess = windowsAuthForUser
                .Databases
                .FirstOrDefault(x => x.TenantId == tenantId);
            if (dataAccess == null)
            {
                dataAccess = new DatabaseAccess
                             {
                                 TenantId = tenantId
                             };
                windowsAuthForUser.Databases.Add(dataAccess);
            }
            dataAccess.ReadOnly = false;
            dataAccess.Admin = true;
        }

        internal class WindowsAuthDocument
        {
            public List<WindowsAuthData> RequiredGroups = new List<WindowsAuthData>();
            public List<WindowsAuthData> RequiredUsers = new List<WindowsAuthData>();
        }

        internal class WindowsAuthData
        {
            public string Name;
            public bool Enabled;
            public List<DatabaseAccess> Databases = new List<DatabaseAccess>();
        }

        internal class DatabaseAccess
        {
            public bool Admin;
            public bool ReadOnly;
            public string TenantId;
        }
    }

}