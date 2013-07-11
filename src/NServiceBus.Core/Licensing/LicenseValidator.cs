namespace NServiceBus.Licensing
{
    using System;
    using Rhino.Licensing;

    /// <summary>
    /// Validates content of a license file
    /// </summary>
    internal class StringLicenseValidator : AbstractLicenseValidator
    {
        /// <summary>
        /// Creates a new instance of <seealso cref="StringLicenseValidator"/>
        /// </summary>
        /// <param name="publicKey">public key</param>
        /// <param name="license">license content</param>
        public StringLicenseValidator(string publicKey, string license)
            : base(publicKey)
        {
            License = license;
        }

        /// <summary>
        /// License content
        /// </summary>
        protected override sealed string License
        {
            get;
            set;
        }

        public override void AssertValidLicense()
        {
            if (String.IsNullOrEmpty(License))
            {
                throw new LicenseNotFoundException();
            }

            base.AssertValidLicense();
        }
    }
}