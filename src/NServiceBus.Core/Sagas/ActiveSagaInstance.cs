namespace NServiceBus.Sagas
{
    using System;
    using Saga;

    public class ActiveSagaInstance
    {
        public ActiveSagaInstance(Saga saga)
        {
            Instance = saga;
            SagaType = saga.GetType();
        }

        public Type SagaType { get; private set; }
        
        public Saga Instance { get; private set; }
        
        public bool IsNew { get; private set; }
                     
        public bool NotFound { get; private set; }

        public void AttachNewEntity(IContainSagaData sagaEntity)
        {
            IsNew = true;
            AttachEntity(sagaEntity);
        }

        
        public void AttachExistingEntity(IContainSagaData loadedEntity)
        {
            AttachEntity(loadedEntity);
        }

        void AttachEntity(IContainSagaData sagaEntity)
        {
            Instance.Entity = sagaEntity;
        }

        public void MarkAsNotFound()
        {
            NotFound = true;
        }
    }
}