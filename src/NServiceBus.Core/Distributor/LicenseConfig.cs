namespace NServiceBus.Distributor
{
    /// <summary>
    /// Limit number of workers in accordance with Licensing policy
    /// </summary>
    [ObsoleteEx(Message = "Not a public API.", TreatAsErrorFromVersion = "4.3", RemoveInVersion = "5.0")]
    public static class LicenseConfig
    {
        internal static bool LimitNumberOfWorkers(Address workerAddress)
        {
            return false;
        }
    }
}
