namespace NServiceBus.Core.Tests.Persistence
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Persistence;
    using NServiceBus.Persistence.Legacy;
    using NServiceBus.Settings;
    using NUnit.Framework;

    [TestFixture]
    public class When_configuring_storage_type_not_supported_by_persistence
    {
        [Test]
        public void Should_throw_exception()
        {
            // ReSharper disable once ObjectCreationAsStatement
            var ex = Assert.Throws<Exception>(() => new PersistenceExtentions(typeof(MsmqPersistence), new SettingsHolder(), typeof(StorageType.Timeouts)));
            Assert.That(ex.Message, Is.EqualTo("Persistence 'MsmqPersistence' does not support storage type Timeouts."));
        }
    }

    [TestFixture]
    public class When_configuring_storage_type_supported_by_persistence
    {
        [Test]
        public void Should_not_throw_exception()
        {
            // ReSharper disable once ObjectCreationAsStatement
            Assert.DoesNotThrow(() => new PersistenceExtentions(typeof(MsmqPersistence), new SettingsHolder(), typeof(StorageType.Subscriptions)));
        }
    }
}