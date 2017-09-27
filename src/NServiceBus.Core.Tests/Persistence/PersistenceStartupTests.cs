namespace NServiceBus.Core.Tests.Persistence
{
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    public class When_no_persistence_has_been_configured
    {
        [Test]
        public void Should_return_false_when_checking_if_persistence_supports_storage_type()
        {
            var settings = new SettingsHolder();

            var supported = PersistenceStartup.HasSupportFor<StorageType.Subscriptions>(settings);

            Assert.IsFalse(supported);
        }
    }
}
