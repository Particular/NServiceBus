namespace NServiceBus.Licensing
{
    using System;

    /// <summary>
    /// NServiceBus License information
    /// </summary>
    public class License
    {
        public string LicenseType { get; set; }
        public int MaxThroughputPerSecond { get; internal set; }
        public int AllowedNumberOfThreads { get; internal set; }
        public int AllowedNumberOfWorkerNodes { get; internal set; }
        public DateTime ExpirationDate { get; internal set; }
    }
}
