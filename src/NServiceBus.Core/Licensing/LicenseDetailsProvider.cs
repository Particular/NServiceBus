namespace NServiceBus.Licensing
{
    class LicenseDetailsProvider
    {
        public string LicenseId { get; }
        public string CustomerName { get; }

        public LicenseDetailsProvider(string licenseId, string customerName)
        {
            LicenseId = licenseId;
            CustomerName = customerName;
        }
    }
}