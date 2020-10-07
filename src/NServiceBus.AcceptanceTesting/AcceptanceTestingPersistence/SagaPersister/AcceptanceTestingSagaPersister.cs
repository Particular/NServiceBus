namespace NServiceBus.AcceptanceTesting.AcceptanceTestingPersistence.SagaPersister
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;
    using Sagas;

    class AcceptanceTestingSagaPersister : ISagaPersister
    {
        public AcceptanceTestingSagaPersister()
        {
            sagas = new ConcurrentDictionary<Guid, Entry>();
            sagasCollection = sagas;
            byCorrelationId = new ConcurrentDictionary<CorrelationId, Guid>();
            byCorrelationIdCollection = byCorrelationId;
        }

        public Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            ((AcceptanceTestingSynchronizedStorageSession)session).Enlist(() =>
           {
               var entry = GetEntry(context, sagaData.Id);

               if (sagasCollection.Remove(new KeyValuePair<Guid, Entry>(sagaData.Id, entry)) == false)
               {
                   throw new Exception("Saga can't be completed as it was updated by another process.");
               }

               // saga removed
               // clean the index
               if (Equals(entry.CorrelationId, NoCorrelationId) == false)
               {
                   byCorrelationIdCollection.Remove(new KeyValuePair<CorrelationId, Guid>(entry.CorrelationId, sagaData.Id));
               }
           });

            return Task.CompletedTask;
        }

        public Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context)
            where TSagaData : class, IContainSagaData
        {
            if (sagas.TryGetValue(sagaId, out var value))
            {
                SetEntry(context, sagaId, value);

                var data = value.GetSagaCopy();
                return Task.FromResult((TSagaData)data);
            }

            return CachedSagaDataTask<TSagaData>.Default;
        }

        public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context)
            where TSagaData : class, IContainSagaData
        {
            var key = new CorrelationId(typeof(TSagaData), propertyName, propertyValue);

            if (byCorrelationId.TryGetValue(key, out var id))
            {
                // this isn't updated atomically and may return null for an entry that has been indexed but not inserted yet
                return Get<TSagaData>(id, session, context);
            }

            return CachedSagaDataTask<TSagaData>.Default;
        }

        public Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            ((AcceptanceTestingSynchronizedStorageSession)session).Enlist(() =>
           {
               var correlationId = NoCorrelationId;
               if (correlationProperty != SagaCorrelationProperty.None)
               {
                   correlationId = new CorrelationId(sagaData.GetType(), correlationProperty);
                   if (byCorrelationId.TryAdd(correlationId, sagaData.Id) == false)
                   {
                       throw new InvalidOperationException($"The saga with the correlation id 'Name: {correlationProperty.Name} Value: {correlationProperty.Value}' already exists");
                   }
               }

               var entry = new Entry(sagaData, correlationId);
               if (sagas.TryAdd(sagaData.Id, entry) == false)
               {
                   throw new Exception("A saga with this identifier already exists. This should never happened as saga identifier are meant to be unique.");
               }
           });

            return Task.CompletedTask;
        }

        public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            ((AcceptanceTestingSynchronizedStorageSession)session).Enlist(() =>
           {
               var entry = GetEntry(context, sagaData.Id);

               if (sagas.TryUpdate(sagaData.Id, entry.UpdateTo(sagaData), entry) == false)
               {
                   throw new Exception($"AcceptanceTestingSagaPersister concurrency violation: saga entity Id[{sagaData.Id}] already saved.");
               }
           });

            return Task.CompletedTask;
        }

        static void SetEntry(ContextBag context, Guid sagaId, Entry value)
        {
            if (context.TryGet(ContextKey, out Dictionary<Guid, Entry> entries) == false)
            {
                entries = new Dictionary<Guid, Entry>();
                context.Set(ContextKey, entries);
            }
            entries[sagaId] = value;
        }

        static Entry GetEntry(ReadOnlyContextBag context, Guid sagaDataId)
        {
            if (context.TryGet(ContextKey, out Dictionary<Guid, Entry> entries))
            {
                if (entries.TryGetValue(sagaDataId, out var entry))
                {
                    return entry;
                }
            }
            throw new Exception("The saga should be retrieved with Get method before it's updated");
        }

        readonly ConcurrentDictionary<Guid, Entry> sagas;
        readonly ConcurrentDictionary<CorrelationId, Guid> byCorrelationId;
        readonly ICollection<KeyValuePair<Guid, Entry>> sagasCollection;
        readonly ICollection<KeyValuePair<CorrelationId, Guid>> byCorrelationIdCollection;
        const string ContextKey = "NServiceBus.AcceptanceTestingSagaPersistence.Sagas";
        static readonly CorrelationId NoCorrelationId = new CorrelationId(typeof(object), "", new object());

        class Entry
        {
            static Entry()
            {
                var func = GenerateMemberwiseClone();
                shallowCopy = sagaData => (IContainSagaData)func(sagaData);
            }

            public Entry(IContainSagaData sagaData, CorrelationId correlationId)
            {
                CorrelationId = correlationId;
                data = sagaData;
            }

            public CorrelationId CorrelationId { get; }

            static IContainSagaData DeepCopy(IContainSagaData source)
            {
                return source.DeepCopy();
            }

            public IContainSagaData GetSagaCopy()
            {
                // ReSharper disable once ConvertClosureToMethodGroup, no allocations are needed!
                var canBeShallowCopied = canBeShallowCopiedCache.GetOrAdd(data.GetType(), type => CanBeShallowCopied(type));
                return canBeShallowCopied ? shallowCopy(data) : DeepCopy(data);
            }

            static bool CanBeShallowCopied(Type type)
            {
                foreach (var fi in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy))
                {
                    var fieldType = fi.FieldType;

                    if (fieldType.IsPrimitive == false)
                    {
                        if (fieldType != typeof(string) && fieldType != typeof(Guid))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            static Func<object, object> GenerateMemberwiseClone()
            {
                var method = new DynamicMethod("CloneMemberwise", typeof(object), new[]
                {
                    typeof(object)
                }, typeof(object).Assembly.ManifestModule, true);
                var ilGenerator = method.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                var methodInfo = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                ilGenerator.EmitCall(OpCodes.Call, methodInfo, null);
                ilGenerator.Emit(OpCodes.Ret);

                return (Func<object, object>)method.CreateDelegate(typeof(Func<object, object>));
            }

            public Entry UpdateTo(IContainSagaData sagaData)
            {
                return new Entry(sagaData, CorrelationId);
            }

            readonly IContainSagaData data;
            static ConcurrentDictionary<Type, bool> canBeShallowCopiedCache = new ConcurrentDictionary<Type, bool>();
            static Func<IContainSagaData, IContainSagaData> shallowCopy;
        }

        /// <summary>
        /// This correlation id is cheap to create as type and the propertyName are not allocated (they are stored in the saga
        /// metadata).
        /// The only thing that is allocated is the correlationId itself and the propertyValue, which again, is allocated anyway
        /// by the saga behavior.
        /// </summary>
        class CorrelationId
        {
            public CorrelationId(Type type, string propertyName, object propertyValue)
            {
                this.type = type;
                this.propertyName = propertyName;
                this.propertyValue = propertyValue;
            }

            public CorrelationId(Type sagaType, SagaCorrelationProperty correlationProperty)
                : this(sagaType, correlationProperty.Name, correlationProperty.Value)
            {
            }

            bool Equals(CorrelationId other)
            {
                return type == other.type && string.Equals(propertyName, other.propertyName) && propertyValue.Equals(other.propertyValue);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((CorrelationId)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    // propertyName isn't taken into consideration as there will be only one property per saga to correlate.
                    var hashCode = type.GetHashCode();
                    hashCode = (hashCode * 397) ^ propertyValue.GetHashCode();
                    return hashCode;
                }
            }

            readonly Type type;
            readonly string propertyName;
            readonly object propertyValue;
        }
    }

    static class CachedSagaDataTask<TSagaData>
                    where TSagaData : IContainSagaData
    {
        public static Task<TSagaData> Default = Task.FromResult(default(TSagaData));
    }
}