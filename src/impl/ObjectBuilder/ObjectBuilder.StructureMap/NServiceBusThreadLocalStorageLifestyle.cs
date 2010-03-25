using StructureMap.Pipeline;

namespace NServiceBus.ObjectBuilder.StructureMap
{
    public class NServiceBusThreadLocalStorageLifestyle : ThreadLocalStorageLifecycle,  IMessageModule
    {
        public void HandleBeginMessage(){}

        public void HandleEndMessage()
        {
            EjectAll();
        }

        public void HandleError(){}
    }
}