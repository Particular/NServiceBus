namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using ObjectBuilder;
    using Transports;

    class SatelliteDefinition
    {
        public SatelliteDefinition(string name, string receiveAddress, TransportTransactionMode requiredTransportTransactionMode, PushRuntimeSettings runtimeSettings, 
            Func<IBuilder,PushContext, Task> onMessage,
            Func<IBuilder, PushContext, Exception, int, Task<bool>> onError)
        {
            Name = name;
            ReceiveAddress = receiveAddress;
            RequiredTransportTransactionMode = requiredTransportTransactionMode;
            RuntimeSettings = runtimeSettings;
            OnMessage = onMessage;
            OnError = onError;
        }

        public string Name { get; private set; }

        public string ReceiveAddress { get; private set; }

        public TransportTransactionMode RequiredTransportTransactionMode { get; private set; }

        public PushRuntimeSettings RuntimeSettings { get; private set; }

        public Func<IBuilder,PushContext,Task> OnMessage{ get; private set; }

        public Func<IBuilder, PushContext,Exception,int, Task<bool>> OnError { get; private set; }
    }
}