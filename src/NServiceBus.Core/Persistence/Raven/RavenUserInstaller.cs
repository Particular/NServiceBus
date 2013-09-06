namespace NServiceBus.Persistence.Raven
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using global::Raven.Abstractions.Extensions;
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

            systemCommands.Put("Raven/Authorization/WindowsSettings", null, ravenJObject, new RavenJObject());
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