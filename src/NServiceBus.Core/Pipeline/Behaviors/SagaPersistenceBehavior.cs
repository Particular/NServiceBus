namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Saga;

    public class SagaPersistenceBehavior : IBehavior
    {
        public IBehavior Next { get; set; }

        public ISagaPersister SagaPersister { get; set; }

        public SagaMetaDataRegistry SagaMetaDataRegistry { get; set; }

        public void Invoke(IBehaviorContext context)
        {
            var activeSagaInstances = GetOrCreateActiveSagaInstances(context);
            var messages = context.Messages;

            foreach (var message in messages)
            {
                LoadSagaInstancesForMessage(message, activeSagaInstances);
            }

            Next.Invoke(context);

            foreach (var sagaInstanceContainer in activeSagaInstances.Instances)
            {
                //SagaPersister.Update(theInstances);
            }
        }

        void LoadSagaInstancesForMessage(object message, ActiveSagaInstances activeSagaInstances)
        {
            var messageType = message.GetType();

            // this would be how we discovered WhateverSagaData
            // this one would return multiple types because a message may have multiple (saga)handlers
            // note: add an acceptance test for this scenario
            var sagaDataTypes = SagaMetaDataRegistry.GetSagaDataTypesFor(messageType);

            foreach (var sagaDataType in sagaDataTypes)
            {
                var correlationPropertyName = SagaMetaDataRegistry.GetCorrelationPropertyName(sagaDataType,
                                                                                              messageType);

                // this one is probably Func<TMessage, object>
                var messageFieldExtractor = SagaMetaDataRegistry.GetPropertyValueMethod(sagaDataType, messageType);

                IContainSagaData sagaEntity = SagaPersister.Get<WhateverSagaData>(correlationPropertyName,
                                                                                  messageFieldExtractor(message));

                var isNew = false;
                if (sagaEntity == null)
                {
                    sagaEntity = CreateNew<WhateverSagaData>();
                    isNew = true;
                }

                activeSagaInstances.AddInstance(new SagaInstanceContainer
                                                    {
                                                        ContainSagaData = sagaEntity,
                                                        SagaInstanceStateChangeDescriptor =
                                                            isNew
                                                                ? SagaInstanceStateChangeDescriptor.New
                                                                : SagaInstanceStateChangeDescriptor.Existing
                                                    });
            }
        }

        static ActiveSagaInstances GetOrCreateActiveSagaInstances(IBehaviorContext context)
        {
            var activeSagaInstances = context.Get<ActiveSagaInstances>();
            if (activeSagaInstances != null)
            {
                return activeSagaInstances;
            }

            activeSagaInstances = new ActiveSagaInstances();
            context.Set(activeSagaInstances);
            return activeSagaInstances;
        }

        IContainSagaData CreateNew<T>() where T : IContainSagaData
        {
            // assign new ID and probably some more stuff
            return (IContainSagaData)null;
        }
    }

    public class ActiveSagaInstances
    {
        public ActiveSagaInstances()
        {
            // Tuple<IContainSagaData, bool> = (data, whetherIsIsNew)
            Instances = new List<SagaInstanceContainer>();
        }

        public List<SagaInstanceContainer> Instances { get; set; }

        public void AddInstance(SagaInstanceContainer instanceContainer)
        {
            Instances.Add(instanceContainer);
        }
    }

    public enum SagaInstanceStateChangeDescriptor
    {
        New, Existing, Completed
    }

    public class SagaInstanceContainer
    {
        public IContainSagaData ContainSagaData { get; set; }
        public SagaInstanceStateChangeDescriptor SagaInstanceStateChangeDescriptor { get; set; }
    }

    public class WhateverSagaData : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }

    /// <summary>
    /// Registry for metadata about sagas
    /// </summary>
    public class SagaMetaDataRegistry
    {
        readonly ConcurrentDictionary<Type, Type[]> sagaDataTypesByIncomingMessageType 
            = new ConcurrentDictionary<Type, Type[]>();
        
        readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, Correlator>> correlationPropertyBySagaDataTypeAndMessageType
            = new ConcurrentDictionary<Type, ConcurrentDictionary<Type, Correlator>>();

        public Type[] GetSagaDataTypesFor(Type messageType)
        {
            Type[] types;
            return sagaDataTypesByIncomingMessageType.TryGetValue(messageType, out types)
                       ? types
                       : new Type[0];
        }

        public string GetCorrelationPropertyName(Type sagaDataType, Type messageType)
        {
            ConcurrentDictionary<Type, Correlator> secondIndex;
            Correlator correlator;

            return correlationPropertyBySagaDataTypeAndMessageType
                       .TryGetValue(sagaDataType, out secondIndex)
                       ? secondIndex.TryGetValue(messageType, out correlator)
                             ? correlator.PropertyName
                             : null
                       : null;
        }

        public Func<object, object> GetPropertyValueMethod(Type sagaDataType, Type messageType)
        {
            ConcurrentDictionary<Type, Correlator> secondIndex;
            Correlator correlator;

            return correlationPropertyBySagaDataTypeAndMessageType
                       .TryGetValue(sagaDataType, out secondIndex)
                       ? secondIndex.TryGetValue(messageType, out correlator)
                             ? correlator.MessageFieldExtractor
                             : null
                       : null;
        }

        class Correlator
        {
            public string PropertyName { get; set; }
            public Func<object, object> MessageFieldExtractor { get; set; }
        }
    }
}