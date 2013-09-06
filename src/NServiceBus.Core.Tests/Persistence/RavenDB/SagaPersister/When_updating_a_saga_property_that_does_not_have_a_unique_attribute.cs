namespace NServiceBus.Core.Tests.Persistence.RavenDB.SagaPersister
{
    using System;
    using NServiceBus.Persistence.Raven.SagaPersister;
    using NUnit.Framework;

    public class When_updating_a_saga_property_that_does_not_have_a_unique_attribute : Raven_saga_persistence_concern
    {
        [Test]
        public void It_should_persist_successfully()
        {
            var uniqueString = Guid.NewGuid().ToString();

            var saga1 = new SagaWithUniqueProperty
                        {
                            Id = Guid.NewGuid(),
                            UniqueString = uniqueString,
                            NonUniqueString = "notUnique"
                        };

            SaveSaga(saga1);

            UpdateSaga<SagaWithUniqueProperty>(saga1.Id, s => s.NonUniqueString = "notUnique2");
        }
    }

    public class When_updating_a_saga_property_on_a_existing_sagainstance_that_just_got_a_unique_attribute_set : Raven_saga_persistence_concern
    {
        [Test]
        public void It_should_set_the_attribute_and_allow_the_update()
        {
            var uniqueString = Guid.NewGuid().ToString();

            var anotherUniqueString = Guid.NewGuid().ToString();

            var saga1 = new SagaWithUniqueProperty
                        {
                Id = Guid.NewGuid(),
                UniqueString = uniqueString,
                NonUniqueString = "notUnique"
            };

            SaveSaga(saga1);
            
            using(var session = store.OpenSession())
            {
                //fake that the attribute was just added by removing the metadata
                session.Advanced.GetMetadataFor(saga1).Remove(RavenSagaPersister.UniqueValueMetadataKey);
                session.SaveChanges();
            }

            UpdateSaga<SagaWithUniqueProperty>(saga1.Id, s => s.UniqueString = anotherUniqueString);

            string value;

            using (var session = store.OpenSession())
                value = session.Advanced.GetMetadataFor(saga1)[RavenSagaPersister.UniqueValueMetadataKey].ToString();

            Assert.AreEqual(anotherUniqueString, value);

        }
    }


    public class When_updating_a_saga_without_unique_properties : Raven_saga_persistence_concern
    {
        [Test]
        public void It_should_persist_successfully()
        {
            var uniqueString = Guid.NewGuid().ToString();
            var anotherUniqueString = Guid.NewGuid().ToString();

            var saga1 = new SagaWithoutUniqueProperties
                        {
                Id = Guid.NewGuid(),
                UniqueString = uniqueString,
                NonUniqueString = "notUnique"
            };

            SaveSaga(saga1);

            UpdateSaga<SagaWithoutUniqueProperties>(saga1.Id, s =>
                                                             {
                                                                 s.NonUniqueString = "notUnique2";
                                                                 s.UniqueString = anotherUniqueString;
                                                             });
        }
    }
}