namespace NServiceBus.Core.Tests.Installers
{
    using System;
    using Installation;
    using NUnit.Framework;
    using Raven.Client.Document;
    using Raven.Client.Embedded;

    [TestFixture]
    public class RavenUserInstallerTests
    {
        [Test]
        [Explicit]
        //this will edit your current installed RavenDB
        public void Integration()
        {
            using (var documentStore = new DocumentStore
                                       {
                                           Url = "http://localhost:8080"
                                       })
            {
                documentStore.Initialize();
                var identity = Environment.MachineName + @"\nsbtest";
                RavenUserInstaller.AddUserToDatabase(identity, documentStore, "Test");
            }
        }

        [Test]
        public void EndureUserIsAddedToWindowsSettings()
        {
            using (var documentStore = new EmbeddableDocumentStore
                                       {
                                           RunInMemory = true
                                       })
            {
                documentStore.Initialize();
                RavenUserInstaller.AddUserToDatabase(@"domain\user", documentStore, "FakeDatabase");
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
          ""TenantId"": ""FakeDatabase""
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
