namespace NServiceBus.Licensing
{
    using System;

    /// <summary>
    /// NServiceBus License information
    /// </summary>
    class License
    {
        public int MaxThroughputPerSecond;
        public int AllowedNumberOfWorkerNodes;
        public DateTime ExpirationDate;
        public string Name;
        public DateTime? UpgradeProtectionExpiration;
        public string LicenseVersion;
        public Guid UserId;
    }
}
