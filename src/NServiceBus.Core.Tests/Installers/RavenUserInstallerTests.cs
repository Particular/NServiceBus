namespace NServiceBus.Core.Tests.Installers
{
    using System;
    using NServiceBus.Persistence.Raven;
    using NUnit.Framework;
    using Raven.Client.Document;
    using Raven.Client.Embedded;

    [TestFixture]
    public class RavenUserInstallerTests
    {
        [Test]
        [Explicit("this will edit your current installed RavenDB")]
        public void Integration()
        {
            using (var documentStore = new DocumentStore
                                       {
                                           Url = "http://localhost:8080",
                                           DefaultDatabase = "Test"
                                       })
            {
                documentStore.Initialize();

                var identity = Environment.MachineName + @"\Test";
                RavenUserInstaller.AddUserToDatabase(identity, documentStore);
            }
        }

        [Test]
        public void EnsureUserIsAddedToWindowsSettings()
        {
            using (var documentStore = new EmbeddableDocumentStore
                                       {
                                           RunInMemory = true,
                                       })
            {
                documentStore.Initialize();
                RavenUserInstaller.AddUserToDatabase(@"domain\user", documentStore);
                var systemCommands = documentStore
                    .DatabaseCommands
                    .ForSystemDatabase();
                var existing = systemCommands.Get("Raven/Authorization/WindowsSettings");

                var expected = @"{
  ""RequiredGroups"": [],
  ""RequiredUsers"": [
    {
      ""Name"": ""domain\\user"",
      ""Enabled"": true,
      ""Databases"": [
        {
          ""Admin"": true,
          ""ReadOnly"": false,
          ""TenantId"": ""<system>""
        }
      ]
    }
  ]
}";
                var actual = existing.DataAsJson.ToString();
                Assert.AreEqual(expected, actual);
            }

        }
    }

}
