using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;

namespace NServiceBus.Claims
{
    public static class FindCertificates
    {
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public static X509Certificate2 ByThumbPrint(StoreName name, StoreLocation location, string thumbprint)
        {
            var store = new X509Store(name, location);
            X509Certificate2Collection certificates = null;
            store.Open(OpenFlags.ReadOnly);

            try
            {
                certificates = store.Certificates;

                var result = (from X509Certificate2 cert in certificates 
                              where cert.SubjectName.Name != null 
                              where cert.Thumbprint != null && cert.Thumbprint.Equals(thumbprint, StringComparison.OrdinalIgnoreCase) 
                              select cert)
                            .Select(cert => new X509Certificate2(cert))
                            .FirstOrDefault();

                if (result == null)
                {
                    throw new CryptographicException(string.Format(CultureInfo.CurrentUICulture, "No certificate was found for thumbprint {0}", thumbprint));
                }

                return result;
            }
            finally
            {
                if (certificates != null)
                {
                    foreach (var cert in certificates) cert.Reset();
                }

                store.Close();
            }
        }
    }
}