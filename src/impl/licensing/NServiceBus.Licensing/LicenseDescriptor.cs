using System;
using System.IO;

namespace NServiceBus.Licensing
{
    public class LicenseDescriptor
    {
        public static string LocalLicenseFile
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"License\License.xml"); }
        }

        public static string PublicKey
        {
            get
            {
                return @"<RSAKeyValue><Modulus>5M9/p7N+JczIN/e5eObahxeCIe//2xRLA9YTam7zBrcUGt1UlnXqL0l/8uO8rsO5tl+tjjIV9bOTpDLfx0H03VJyxsE8BEpSVu48xujvI25+0mWRnk4V50bDZykCTS3Du0c8XvYj5jIKOHPtU//mKXVULhagT8GkAnNnMj9CvTc=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
            }
        }
    }
}