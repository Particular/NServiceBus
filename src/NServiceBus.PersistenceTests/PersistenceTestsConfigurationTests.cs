namespace NServiceBus.PersistenceTesting;

using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public class PersistenceTestsConfigurationTests
{
    [Test]
    public async Task Cleanup_should_remove_the_saga_storage_directory()
    {
        var configuration = new PersistenceTestsConfiguration(new TestVariant("default"));

        await configuration.Configure();

        var storageLocation = Path.Combine(Path.GetTempPath(), ".sagas", TestContext.CurrentContext.Test.ID);
        Assert.That(Directory.Exists(storageLocation), Is.True, "Configure should create the saga storage directory");

        await configuration.Cleanup();

        Assert.That(Directory.Exists(storageLocation), Is.False, "Cleanup should remove the saga storage directory");
    }

    [Test]
    public async Task Cleanup_should_not_throw_when_storage_directory_is_missing()
    {
        var configuration = new PersistenceTestsConfiguration(new TestVariant("default"));

        await configuration.Configure();

        var storageLocation = Path.Combine(Path.GetTempPath(), ".sagas", TestContext.CurrentContext.Test.ID);
        Directory.Delete(storageLocation, true);

        Assert.DoesNotThrowAsync(() => configuration.Cleanup());
    }
}
