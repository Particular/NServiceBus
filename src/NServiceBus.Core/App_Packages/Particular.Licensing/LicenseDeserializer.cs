namespace Particular.Licensing
{
    using System;
    using System.Linq;
    using System.Xml;

    static class LicenseDeserializer
    {
        public static License Deserialize(string licenseText)
        {
            var license = new License();
            var doc = new XmlDocument();
            doc.LoadXml(licenseText);


            var applications = doc.SelectSingleNode("/license/@Applications");


            if (applications != null)
            {
                license.ValidApplications.AddRange(applications.Value.Split(';'));
            }

            var upgradeProtectionExpiration = doc.SelectSingleNode("/license/@UpgradeProtectionExpiration");

            if (upgradeProtectionExpiration != null)
            {
                license.UpgradeProtectionExpiration = Parse(upgradeProtectionExpiration.Value);
            }
            else
            {
                var expirationDate = doc.SelectSingleNode("/license/@expiration");

                if (expirationDate != null)
                {
                    license.ExpirationDate = Parse(expirationDate.Value);

                }
            }

            var licenseType = doc.SelectSingleNode("/license/@LicenseType");

            if (licenseType == null)
            {
                licenseType = doc.SelectSingleNode("/license/@type");            
            }

            if (licenseType != null)
            {
                license.LicenseType = licenseType.Value;
            }
          
            var name = doc.SelectSingleNode("/license/name");

            if (name != null)
            {
                license.RegisteredTo = name.InnerText;
            }

            return license;
        }

        static DateTime Parse(string dateStringFromLicense)
        {
            if (string.IsNullOrEmpty(dateStringFromLicense))
            {
                throw new Exception("Invalid datestring found in xml");
            }

            return UniversalDateParser.Parse(dateStringFromLicense.Split('T').First());
        }

    }
}