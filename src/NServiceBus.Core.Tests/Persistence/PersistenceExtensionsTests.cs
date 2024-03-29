﻿namespace NServiceBus.Core.Tests.Persistence;

using System;
using NServiceBus.Persistence;
using Settings;
using NUnit.Framework;

[TestFixture]
public class When_configuring_storage_type_not_supported_by_persistence
{
    [Test]
    public void Should_throw_exception()
    {
        var ex = Assert.Throws<Exception>(() => new PersistenceExtensions(typeof(PartialPersistence), new SettingsHolder(), typeof(StorageType.Sagas)));
        Assert.That(ex.Message, Does.StartWith("PartialPersistence does not support storage type Sagas."));
    }

    public class PartialPersistence : PersistenceDefinition
    {
        public PartialPersistence()
        {
            Supports<StorageType.Subscriptions>(s =>
            {
            });
        }
    }
}

[TestFixture]
public class When_configuring_storage_type_supported_by_persistence
{
    [Test]
    public void Should_not_throw_exception()
    {
        Assert.DoesNotThrow(() => new PersistenceExtensions(typeof(PartialPersistence), new SettingsHolder(), typeof(StorageType.Subscriptions)));
    }

    public class PartialPersistence : PersistenceDefinition
    {
        public PartialPersistence()
        {
            Supports<StorageType.Subscriptions>(s =>
            {
            });
        }
    }
}