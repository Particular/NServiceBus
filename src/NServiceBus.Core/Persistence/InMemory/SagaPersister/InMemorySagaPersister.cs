namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;
    using Sagas;

    class InMemorySagaPersister : ISagaPersister
    {
        public Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var inMemSession = (InMemorySynchronizedStorageSession) session;
            inMemSession.Enlist(() =>
            {
                this.sagaData.Remove(sagaData);
            });
            return TaskEx.CompletedTask;
        }

        public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            Guard.AgainstNull(nameof(propertyValue), propertyValue);

            var prop = typeof(TSagaData).GetProperty(propertyName);
            if (prop == null)
            {
                return Task.FromResult(default(TSagaData));
            }

            var saga = sagaData.Get<TSagaData>(propertyValue);

            if (saga == null)
            {
                return Task.FromResult(default(TSagaData));
            }

            return Task.FromResult(saga.Read<TSagaData>());
        }

        public Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            var saga = sagaData.Get(sagaId, typeof(TSagaData));
            if (saga?.SagaData is TSagaData)
            {
                return Task.FromResult(saga.Read<TSagaData>());
            }
            return Task.FromResult(default(TSagaData));
        }

        public Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            var inMemSession = (InMemorySynchronizedStorageSession) session;
            inMemSession.Enlist(() =>
            {
                this.sagaData.Save(sagaData, correlationProperty);
            });
            return TaskEx.CompletedTask;
        }

        public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var inMemSession = (InMemorySynchronizedStorageSession) session;
            inMemSession.Enlist(() =>
            {
                this.sagaData.Update(sagaData);
            });
            return TaskEx.CompletedTask;
        }

        InMemorySagaPersisterCollection sagaData = new InMemorySagaPersisterCollection();

        class VersionedSagaEntity
        {
            public VersionedSagaEntity(IContainSagaData sagaData, VersionedSagaEntity original = null)
            {
                SagaData = DeepClone(sagaData);
                if (original != null)
                {
                    original.ConcurrencyCheck(sagaData);

                    versionCache = original.versionCache;
                    version = original.version;
                    version++;
                }
                else
                {
                    versionCache = new ConditionalWeakTable<IContainSagaData, SagaVersion>();
                    versionCache.Add(sagaData, new SagaVersion(version));
                }
            }

            public TSagaData Read<TSagaData>()
                where TSagaData : IContainSagaData
            {
                var clone = DeepClone(SagaData);
                versionCache.Add(clone, new SagaVersion(version));
                return (TSagaData) clone;
            }

            void ConcurrencyCheck(IContainSagaData sagaEntity)
            {
                SagaVersion v;
                if (!versionCache.TryGetValue(sagaEntity, out v))
                {
                    throw new Exception($"InMemorySagaPersister in an inconsistent state: entity Id[{sagaEntity.Id}] not read.");
                }

                if (v.Version != version)
                {
                    throw new Exception($"InMemorySagaPersister concurrency violation: saga entity Id[{sagaEntity.Id}] already saved.");
                }
            }

            static IContainSagaData DeepClone(IContainSagaData source)
            {
                var json = serializer.SerializeObject(source);
                return (IContainSagaData) serializer.DeserializeObject(json, source.GetType());
            }

            public IContainSagaData SagaData;

            ConditionalWeakTable<IContainSagaData, SagaVersion> versionCache;

            int version;

            static JsonMessageSerializer serializer = new JsonMessageSerializer(null);

            class SagaVersion
            {
                public SagaVersion(long version)
                {
                    Version = version;
                }

                public long Version;
            }
        }

        class InMemorySagaPersisterCollection
        {
            Dictionary<Guid, VersionedSagaEntity> sagaIdIndex = new Dictionary<Guid, VersionedSagaEntity>();
            Dictionary<Type, Dictionary<string, Guid>> correlationValueIndex = new Dictionary<Type, Dictionary<string, Guid>>();
            Dictionary<Type, string> correlationProperties = new Dictionary<Type, string>();
            Dictionary<string, object> lockers = new Dictionary<string, object>();

            public void Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty)
            {
                var sagaType = sagaData.GetType();

                var lockToken = new object();

                lock (lockToken)
                {

                    if (correlationProperty != SagaCorrelationProperty.None)
                    {
                        SagaPropertyValueIsUnique(sagaType, correlationProperty, sagaData.Id);
                    }

                    if (!correlationProperties.ContainsKey(sagaType))
                    {
                        correlationProperties.Add(sagaType, correlationProperty.Name);
                    }
                    if (!lockers.ContainsKey(sagaData.Id.ToString()))
                    {
                        lockers.Add(sagaData.Id.ToString(), lockToken);
                    }

                    var correlationLockKey = CreateCorrelationLockKey(sagaType, correlationProperty.Value);

                    if (!lockers.ContainsKey(correlationLockKey))
                    {
                        lockers.Add(correlationLockKey, lockToken);
                    }
                    if (!lockers.ContainsKey(sagaType.FullName))
                    {
                        lockers.Add(sagaType.FullName, new object());
                    }

                    sagaIdIndex.Add(sagaData.Id, new VersionedSagaEntity(sagaData));

                    if (!correlationValueIndex.ContainsKey(sagaType))
                    {
                        correlationValueIndex.Add(sagaType, new Dictionary<string, Guid>());
                    }

                    if (correlationValueIndex[sagaType].ContainsKey(correlationProperty.Value.ToString()))
                    {
                        correlationValueIndex[sagaType].Remove(correlationProperty.Value.ToString());
                    }

                    correlationValueIndex[sagaType].Add(correlationProperty.Value.ToString(), sagaData.Id);
                }
            }

            object GetLocker(Guid? sagaId, Type sagaType, object correlationValue)
            {
                var correlationKey = correlationValue == null ? null : CreateCorrelationLockKey(sagaType, correlationValue);

                if (sagaId !=null && lockers.ContainsKey(sagaId.ToString()))
                {
                    return lockers[sagaId.ToString()];
                }
                if (correlationKey != null && lockers.ContainsKey(correlationKey))
                {
                    return lockers[correlationKey];
                }
                if (lockers.ContainsKey(sagaType.FullName))
                {
                    return lockers[sagaType.FullName];
                }

                return new object();
            }

            public void Update(IContainSagaData sagaData)
            {
                var sagaType = sagaData.GetType();
                var correlationProperty = sagaType.GetProperty(correlationProperties[sagaType]);

                var lockToken = GetLocker(sagaData.Id, sagaData.GetType(), null);

                lock (lockToken)
                {
                    var oldSaga = sagaIdIndex[sagaData.Id];
                    var oldCorrelationValue = correlationProperty.GetValue(oldSaga.SagaData);
                    var newCorrelationValue = correlationProperty.GetValue(sagaData);

                    sagaIdIndex[sagaData.Id] = new VersionedSagaEntity(sagaData, oldSaga);

                    correlationValueIndex[sagaType].Remove(oldCorrelationValue.ToString());
                    correlationValueIndex[sagaType].Add(newCorrelationValue.ToString(), sagaData.Id);
                    lockers.Remove(CreateCorrelationLockKey(sagaType, oldCorrelationValue));
                    lockers.Add(CreateCorrelationLockKey(sagaType, newCorrelationValue), lockToken);
                }
            }

            public VersionedSagaEntity Get(Guid sagaId, Type sagaType)
            {
                var lockToken = GetLocker(sagaId, sagaType, null);

                lock (lockToken)
                {
                    VersionedSagaEntity saga;
                    sagaIdIndex.TryGetValue(sagaId, out saga);
                    return saga;
                }
            }

            public VersionedSagaEntity Get<TSagaData>(object propertyValue)
            {
                var lockToken = GetLocker(null, typeof(TSagaData), propertyValue);

                lock(lockToken)
                {
                    Dictionary<string, Guid> sagaTypeCollection;
                    correlationValueIndex.TryGetValue(typeof(TSagaData), out sagaTypeCollection);

                    if (sagaTypeCollection == null)
                    {
                        return null;
                    }

                    Guid sagaId;
                    sagaTypeCollection.TryGetValue(propertyValue.ToString(), out sagaId);

                    if (sagaId == Guid.Empty)
                    {
                        return null;
                    }

                    if (sagaIdIndex.ContainsKey(sagaId))
                    {
                        return sagaIdIndex[sagaId];
                    }

                    return null;
                }
            }

            public void Remove(IContainSagaData sagaData)
            {
                var sagaType = sagaData.GetType();
                var correlationProperty = sagaType.GetProperty(correlationProperties[sagaType]);
                var correlationValue = correlationProperty.GetValue(sagaData);

                var lockToken = GetLocker(sagaData.Id, sagaType, correlationValue);

                lock (lockToken)
                {
                    if (sagaIdIndex.ContainsKey(sagaData.Id))
                    {
                        sagaIdIndex.Remove(sagaData.Id);
                    }
                    if (correlationValueIndex.ContainsKey(sagaType))
                    {
                        correlationValueIndex[sagaType].Remove(correlationValue.ToString());
                    }
                    lockers.Remove(sagaData.Id.ToString());
                    lockers.Remove($"{sagaType.FullName}-{correlationValue}");
                }
            }

            void SagaPropertyValueIsUnique(Type sagaType, SagaCorrelationProperty correlationProperty, Guid sagaId)
            {
                var uniqueProperty = sagaType.GetProperty(correlationProperty.Name);

                if (correlationProperty.Value == null)
                {
                    var message = $"Cannot store saga with id '{sagaId}' since the unique property '{uniqueProperty.Name}' has a null value.";
                    throw new InvalidOperationException(message);
                }

                if (!SagaValueIsUnique(sagaType, correlationProperty))
                {
                    var message = $"Cannot store a saga. The saga with id '{sagaId}' already has property '{uniqueProperty.Name}'.";
                    throw new InvalidOperationException(message);
                }
            }

            bool SagaValueIsUnique(Type sagaType, SagaCorrelationProperty correlationProperty)
            {
                if (!correlationValueIndex.ContainsKey(sagaType))
                {
                    return true;
                }

                return !correlationValueIndex[sagaType].ContainsKey(correlationProperty.Value.ToString());
            }

            string CreateCorrelationLockKey(Type sagaType, object correlationValue)
            {
                return $"{sagaType.FullName}-{correlationValue}";
            }
        }
    }
}