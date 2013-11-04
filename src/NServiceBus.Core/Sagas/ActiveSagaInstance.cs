namespace NServiceBus.Sagas
{
    using System;
    using Pipeline.Behaviors;
    using Saga;

    class ActiveSagaInstance
    {
        public ActiveSagaInstance(ISaga saga, MessageHandler messageHandler, LogicalMessage message)
        {
            Instance = saga;
            SagaType = saga.GetType();
            MessageToProcess = message;
            Handler = messageHandler;

            //default the invocation to disabled until we have a entity attached
            Handler.InvocationDisabled = true;
        }

        public Type SagaType { get; private set; }
        
        public bool Found { get; private set; }
        
        public ISaga Instance { get; private set; }
        
        public bool IsNew { get; private set; }
        
        public LogicalMessage MessageToProcess { get; private set; }
        
        public MessageHandler Handler { get; private set; }
        
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
            Handler.InvocationDisabled = false;
        }

        public void MarkAsNotFound()
        {
            NotFound = true;
        }
    }
}