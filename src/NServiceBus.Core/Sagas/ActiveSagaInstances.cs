namespace NServiceBus.Sagas
{
    using System.Collections.Generic;

    class ActiveSagaInstances
    {
        public ActiveSagaInstances( List<ActiveSagaInstance> instances)
        {
            Instances = instances;
        }

        public List<ActiveSagaInstance> Instances { get; private set; }
    }
}