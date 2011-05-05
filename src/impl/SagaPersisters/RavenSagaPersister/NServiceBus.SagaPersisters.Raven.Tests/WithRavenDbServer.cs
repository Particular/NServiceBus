using System;
using System.IO;
using Raven.Database.Config;
using Raven.Database.Extensions;
using Raven.Http;
using Raven.Server;

namespace NServiceBus.SagaPersisters.Raven.Tests
{
    public class WithRavenDbServer
    {
        protected const string DbDirectory = @".\TestDb\";
        protected const string DbName = DbDirectory + @"DocDb.esb";

        protected RavenDbServer GetNewServer()
        {
            return
                new RavenDbServer(new RavenConfiguration
                {
                    Port = 8080,
                    RunInMemory = true,
                    DataDirectory = "Data",
                    AnonymousUserAccessMode = AnonymousUserAccessMode.All
                });
        }
        
        public WithRavenDbServer()
        {
            try
            {
                new Uri("http://fail/first/time?only=%2bplus");
            }
            catch (Exception)
            {
            }

            ClearDatabaseDirectory();

            Directory.CreateDirectory(DbDirectory);
        }

        protected void ClearDatabaseDirectory()
        {
            IOExtensions.DeleteDirectory(DbName);
            IOExtensions.DeleteDirectory(DbDirectory);
        }
    }
}