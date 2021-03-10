namespace NServiceBus.Core.Tests.Persistence.Learning
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Threading.Tasks;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    [TestFixture]
    public class LearningPersistenceTests
    {
        [Test]
        public async Task Should_deserialize_sub_properties_when_loading_sagas()
        {
            var temp = Path.GetTempPath();
            var storageFolder = Path.Combine(temp, "SagaStorage");
            var metaData = new SagaMetadataCollection();
            metaData.Initialize(new[]
            {
                typeof(SagaUnderTest)
            });

            var sagaManifests = new SagaManifestCollection(metaData, storageFolder, s => s);
            var storage = new LearningSynchronizedStorageSession(sagaManifests);

            var pageId = 10;
            var sagaId = Guid.NewGuid();
            var sagaData = new SagaDataContainer
            {
                Id = sagaId,
                SagaID = sagaId,
                Detail = new Details()
            };
            sagaData.Detail.AddPage(pageId, true);

            storage.Save(sagaData);

            await storage.CompleteAsync();

            storage.Dispose(); //to close the streams

            var readSagaData = await storage.Read<SagaDataContainer>(sagaId);

            Assert.AreEqual(sagaId, readSagaData.Id);
            Assert.NotNull(readSagaData.Detail);
            Assert.NotNull(readSagaData.Detail.Pages);
            Assert.IsTrue(readSagaData.Detail.Pages[pageId]);
        }
    }

    public class SagaUnderTest : Saga<SagaDataContainer>, IAmStartedByMessages<SagaStartMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaDataContainer> mapper)
        {
            mapper.ConfigureMapping<SagaStartMessage>(x => x.ID).ToSaga(s => s.SagaID);
        }

        public Task Handle(SagaStartMessage message, IMessageHandlerContext context)
        {
            return Task.FromResult(0);
        }
    }

    public class SagaStartMessage : ICommand
    {
        public Guid ID { get; }
    }

    public class SagaDataContainer : ContainSagaData
    {
        public Guid SagaID { get; set; }
        public Details Detail { get; set; }
    }

    [Serializable]
    public class Details
    {
        public Details()
        {
            Pages = new ConcurrentDictionary<int, bool>();
        }

        public void AddPage(int pageNo, bool flag)
        {
            Pages[pageNo] = flag;
        }

        public IDictionary<int, bool> Pages { get; set; }
    }

    [Serializable]
    public class DictionaryWrapper : ISerializable
    {
        ConcurrentDictionary<int, bool> inner = new ConcurrentDictionary<int, bool>();

        public bool this[int key]
        {
            get => inner[key];
            set => inner[key] = value;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("inner", inner, typeof(Dictionary<int, bool>));
        }
    }
}