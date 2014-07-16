namespace NServiceBus.Core.Tests.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Persistence;
    using NUnit.Framework;

    [TestFixture]
    public class EnabledPersistencesTests
    {
        [Test]
        public void Should_make_sure_there_is_no_overlap_in_claimed_storages()
        {
            var selected = new EnabledPersistences();

            selected.ClaimStorages(typeof(PersitenceA),new List<Storage>{Storage.Sagas});
            selected.ClaimStorages(typeof(PersitenceB), new List<Storage> { Storage.Outbox });

            Assert.Throws<Exception>(() => selected.ClaimStorages(typeof(PersitenceC), new List<Storage> { Storage.Sagas }));
        }


        [Test]
        public void Should_accumulate_storages()
        {
            var selected = new EnabledPersistences();

            selected.ClaimStorages(typeof(PersitenceA), new List<Storage> { Storage.Sagas });
            selected.ClaimStorages(typeof(PersitenceA), new List<Storage> { Storage.Outbox });

            Assert.AreEqual(2,selected.GetEnabled().First().StoragesToEnable.Count);
        }

        [Test]
        public void Should_remove_duplicates()
        {
            var selected = new EnabledPersistences();

            selected.ClaimStorages(typeof(PersitenceA), new List<Storage> { Storage.Sagas, Storage.Sagas });
            selected.ClaimStorages(typeof(PersitenceA), new List<Storage> { Storage.Sagas });

            Assert.AreEqual(1, selected.GetEnabled().First().StoragesToEnable.Count);
        }

        [Test]
        public void Should_assing_available_storage_to_wildcard_claims()
        {
            var selected = new EnabledPersistences();

            selected.ClaimStorages(typeof(PersitenceA), new List<Storage> { Storage.Sagas});
            selected.AddWildcardRegistration(typeof(PersitenceB), new List<Storage>{Storage.Sagas,Storage.Timeouts});

            Assert.False(selected.GetEnabled().Single(p => p.PersistenceType == typeof(PersitenceB)).StoragesToEnable.Contains(Storage.Sagas));
            Assert.True(selected.GetEnabled().Single(p => p.PersistenceType == typeof(PersitenceB)).StoragesToEnable.Contains(Storage.Timeouts));
            Assert.True(selected.GetEnabled().Single(p => p.PersistenceType == typeof(PersitenceA)).StoragesToEnable.Contains(Storage.Sagas));
        }

        [Test]
        public void Should_provide_status_on_availalable_storages()
        {
            var selected = new EnabledPersistences();

            selected.ClaimStorages(typeof(PersitenceA), new List<Storage> { Storage.Sagas });
            selected.AddWildcardRegistration(typeof(PersitenceB), new List<Storage> { Storage.Timeouts });

            Assert.True(selected.HasSupportFor(Storage.Sagas));
            Assert.True(selected.HasSupportFor(Storage.Timeouts));
            Assert.False(selected.HasSupportFor(Storage.Subscriptions));
        }


        class PersitenceA : PersistenceDefinition
        {
            
        }

        class PersitenceB : PersistenceDefinition
        {

        }

        class PersitenceC : PersistenceDefinition
        {

        }
    }

   
}