using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Threading;
using System.Xml;

namespace Rhino.Licensing
{
    using NServiceBus.Logging;
    using NServiceBus.Logging.Loggers;

    /// <summary>
    /// Base license validator.
    /// </summary>
    public abstract class AbstractLicenseValidator
    {
        /// <summary>
        /// License validator logger
        /// </summary>
        // intentionally suppress logging with a NullLogger to make it easier to upgrade in the future
        protected readonly ILog Log = new NullLogger();

        private readonly string publicKey;
        private readonly Timer nextLeaseTimer;

    	/// <summary>
        /// Fired when license data is invalidated
        /// </summary>
        public event Action<InvalidationType> LicenseInvalidated;

        /// <summary>
        /// Gets the expiration date of the license
        /// </summary>
        public DateTime ExpirationDate
        {
            get; private set;
        }

        /// <summary>
        /// Gets the Type of the license
        /// </summary>
        public LicenseType LicenseType
        {
            get; private set;
        }

        /// <summary>
        /// Gets the Id of the license holder
        /// </summary>
        public Guid UserId
        {
            get; private set;
        }

        /// <summary>
        /// Gets the name of the license holder
        /// </summary>
        public string Name
        {
            get; private set;
        }

        /// <summary>
        /// Gets extra license information
        /// </summary>
        public IDictionary<string, string> LicenseAttributes
        {
            get; private set;
        }

        /// <summary>
        /// Gets or Sets the license content
        /// </summary>
        protected abstract string License
        {
            get; set;
        }

        private void LeaseLicenseAgain(object state)
        {
            if (HasExistingLicense())
                return;

            RaiseLicenseInvalidated();
        }

        private void RaiseLicenseInvalidated()
        {
            var licenseInvalidated = LicenseInvalidated;
            if (licenseInvalidated == null)
                throw new InvalidOperationException("License was invalidated, but there is no one subscribe to the LicenseInvalidated event");
            licenseInvalidated(InvalidationType.TimeExpired);
        }

        /// <summary>
        /// Creates a license validator with specified public key.
        /// </summary>
        /// <param name="publicKey">public key</param>
        protected AbstractLicenseValidator(string publicKey)
        {
        	LeaseTimeout = TimeSpan.FromHours(5);
        	LicenseAttributes = new Dictionary<string, string>();
            nextLeaseTimer = new Timer(LeaseLicenseAgain);
            this.publicKey = publicKey;
        }

        /// <summary>
        /// Validates loaded license
        /// </summary>
        public virtual void AssertValidLicense()
        {
            LicenseAttributes.Clear();
            if (HasExistingLicense())
            {
            	return;
            }

            Log.WarnFormat("Could not validate existing license\r\n{0}", License);
            throw new LicenseNotFoundException();
        }

        private bool HasExistingLicense()
        {
            try
            {
                if (TryLoadingLicenseValuesFromValidatedXml() == false)
                {
                    Log.WarnFormat("Failed validating license:\r\n{0}", License);
                    return false;
                }
                Log.InfoFormat("License expiration date is {0}", ExpirationDate);

                var result = DateTime.UtcNow < ExpirationDate;

                if (!result)
                    throw new LicenseExpiredException("Expiration Date : " + ExpirationDate);

                return true;
            }
            catch (RhinoLicensingException)
            {
                throw;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Loads license data from validated license file.
        /// </summary>
        /// <returns></returns>
        public bool TryLoadingLicenseValuesFromValidatedXml()
        {
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(License);

                if (TryGetValidDocument(publicKey, doc) == false)
                {
                    Log.WarnFormat("Could not validate xml signature of:\r\n{0}", License);
                    return false;
                }

                if (doc.FirstChild == null)
                {
                    Log.WarnFormat("Could not find first child of:\r\n{0}", License);
                    return false;
                }

                var result = ValidateXmlDocumentLicense(doc);
                if (result)
                {
					nextLeaseTimer.Change(LeaseTimeout, LeaseTimeout);
                }
                return result;
            }
            catch (RhinoLicensingException)
            {
                throw;
            }
            catch (Exception e)
            {
                Log.Error("Could not validate license", e);
                return false;
            }
        }

        /// <summary>
        /// Lease timeout
        /// </summary>
    	public TimeSpan LeaseTimeout { get; set; }

        internal bool ValidateXmlDocumentLicense(XmlDocument doc)
        {
            var id = doc.SelectSingleNode("/license/@id");
            if (id == null)
            {
                Log.WarnFormat("Could not find id attribute in license:\r\n{0}", License);
                return false;
            }

            UserId = new Guid(id.Value);

            var date = doc.SelectSingleNode("/license/@expiration");
            if (date == null)
            {
                Log.WarnFormat("Could not find expiration in license:\r\n{0}", License);
                return false;
            }

            ExpirationDate = DateTime.ParseExact(date.Value, "yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture);

            var licenseType = doc.SelectSingleNode("/license/@type");
            if (licenseType == null)
            {
                Log.WarnFormat("Could not find license type in {0}", licenseType);
                return false;
            }

            LicenseType = (LicenseType)Enum.Parse(typeof(LicenseType), licenseType.Value);

            var name = doc.SelectSingleNode("/license/name/text()");
            if (name == null)
            {
                Log.WarnFormat("Could not find licensee's name in license:\r\n{0}", License);
                return false;
            }

            Name = name.Value;

            var license = doc.SelectSingleNode("/license");
            foreach (XmlAttribute attrib in license.Attributes)
            {
                if (attrib.Name == "type" || attrib.Name == "expiration" || attrib.Name == "id")
                    continue;

                LicenseAttributes[attrib.Name] = attrib.Value;
            }

            return true;
        }

        private bool TryGetValidDocument(string licensePublicKey, XmlDocument doc)
        {
            var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(licensePublicKey);

            var nsMgr = new XmlNamespaceManager(doc.NameTable);
            nsMgr.AddNamespace("sig", "http://www.w3.org/2000/09/xmldsig#");

            var signedXml = new SignedXml(doc);
            var sig = (XmlElement)doc.SelectSingleNode("//sig:Signature", nsMgr);
            if (sig == null)
            {
                Log.WarnFormat("Could not find this signature node on license:\r\n{0}", License);
                return false;
            }
            signedXml.LoadXml(sig);

            return signedXml.CheckSignature(rsa);
        }
    }
}