namespace NServiceBus.Licensing
{
    using System;

    /// <summary>
    /// NServiceBus License information
    /// </summary>
    [ObsoleteEx(Message = "Not a public API.", TreatAsErrorFromVersion = "4.5", RemoveInVersion = "5.0")]
    public class License
    {
        public int MaxThroughputPerSecond { get; internal set; }
        public int AllowedNumberOfWorkerNodes { get; internal set; }
        public DateTime ExpirationDate { get; internal set; }
        public DateTime? UpgradeProtectionExpiration { get; internal set; }
    }
}
