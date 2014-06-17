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
        public void Should_make_sure_there_is_no_overlap_in_selected_storages()
        {
            var selected = new EnabledPersistences();

            selected.Add(typeof(PersitenceA),new List<Storage>{Storage.Sagas});
            selected.Add(typeof(PersitenceB), new List<Storage> { Storage.Outbox });

            Assert.Throws<Exception>(() => selected.Add(typeof(PersitenceC), new List<Storage> { Storage.Sagas }));
        }


        [Test]
        public void Should_accumulate_storages()
        {
            var selected = new EnabledPersistences();

            selected.Add(typeof(PersitenceA), new List<Storage> { Storage.Sagas });
            selected.Add(typeof(PersitenceA), new List<Storage> { Storage.Outbox });

            Assert.AreEqual(2,selected.GetEnabled().First().StoragesToEnable.Count);
        }

        [Test]
        public void Should_remove_duplicates()
        {
            var selected = new EnabledPersistences();

            selected.Add(typeof(PersitenceA), new List<Storage> { Storage.Sagas, Storage.Sagas });
            selected.Add(typeof(PersitenceA), new List<Storage> { Storage.Sagas });

            Assert.AreEqual(1, selected.GetEnabled().First().StoragesToEnable.Count);
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