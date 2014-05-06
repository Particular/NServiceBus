namespace NServiceBus.Sagas
{
    using System;
    using System.ComponentModel;
    using Saga;


    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
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