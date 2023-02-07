namespace NServiceBus.Features
{
    using System;

    class LicenseDetailsProvider : ILicenseDetailsProvider
    {
        public Guid LicenseId { get; }
        public string CustomerName { get; }

        public LicenseDetailsProvider(Guid licenseId, string customerName)
        {
            LicenseId = licenseId;
            CustomerName = customerName;
        }
    }

    interface ILicenseDetailsProvider
    {
        Guid LicenseId { get; }
        string CustomerName { get; }
    }
}