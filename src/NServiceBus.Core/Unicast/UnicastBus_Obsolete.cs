// ReSharper disable UnusedParameter.Global
namespace NServiceBus.Unicast
{
    using System;

    public partial class UnicastBus
    {
        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Message = "InMemory.Raise has been removed from the core please see http://docs.particular.net/nservicebus/inmemoryremoval")]
        public void Raise<T>(Action<T> messageConstructor)
        {
            ThrowInMemoryException();
        }

        [ObsoleteEx(RemoveInVersion = "6", TreatAsErrorFromVersion = "5", Message = "InMemory.Raise has been removed from the core please see http://docs.particular.net/nservicebus/inmemoryremoval")]
        public void Raise<T>(T @event)
        {
            ThrowInMemoryException();
        }

        static void ThrowInMemoryException()
        {
            throw new Exception("InMemory.Raise has been removed from the core please see http://docs.particular.net/nservicebus/inmemoryremoval");
        }
    }
}
