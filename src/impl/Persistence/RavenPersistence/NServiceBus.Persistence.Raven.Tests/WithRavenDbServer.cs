using System;
using System.IO;
using Raven.Database.Extensions;

namespace NServiceBus.Persistence.Raven.Tests
{
    public class WithRavenDbServer
    {
        protected const string DbDirectory = @".\TestDb\";
        protected const string DbName = DbDirectory + @"DocDb.esb";

        
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