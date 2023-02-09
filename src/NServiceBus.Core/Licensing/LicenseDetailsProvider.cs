namespace NServiceBus.Features
{
    class LicenseDetailsProvider : ILicenseDetailsProvider
    {
        public string LicenseId { get; }
        public string CustomerName { get; }

        public LicenseDetailsProvider(string licenseId, string customerName)
        {
            LicenseId = licenseId;
            CustomerName = customerName;
        }
    }

    interface ILicenseDetailsProvider
    {
        string LicenseId { get; }
        string CustomerName { get; }
    }
}