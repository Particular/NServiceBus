namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using ObjectBuilder;
    using Transport;

    class SatelliteDefinition : ISatellite
    {
        public SatelliteDefinition(string name, string receiveAddress, TransportTransactionMode requiredTransportTransactionMode, PushRuntimeSettings runtimeSettings, Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> recoverabilityPolicy, Func<IBuilder, MessageContext, Task> onMessage)
        {
            Name = name;
            ReceiveAddress = receiveAddress;
            RequiredTransportTransactionMode = requiredTransportTransactionMode;
            RuntimeSettings = runtimeSettings;
            RecoverabilityPolicy = recoverabilityPolicy;
            OnMessage = onMessage;
        }

        public string Name { get; }

        public string ReceiveAddress { get; }

        public TransportTransactionMode RequiredTransportTransactionMode { get; }

        public PushRuntimeSettings RuntimeSettings { get; }

        public Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> RecoverabilityPolicy { get; }

        public Func<IBuilder, MessageContext, Task> OnMessage { get; }

        public Func<Task> ResumeAction { get; set; } = () => Task.FromResult(0);
        public Func<Task> StopAction { get; set; } = () => Task.FromResult(0);
        
        public Task Resume()
        {
            return ResumeAction();
        }

        public Task Stop()
        {
            return StopAction();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public interface ISatellite
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task Resume();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task Stop();
    }
}