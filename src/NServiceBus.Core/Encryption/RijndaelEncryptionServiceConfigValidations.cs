namespace NServiceBus
{
    using System.Linq;
    using Config;

    static class RijndaelEncryptionServiceConfigValidations
    {
        public static bool ConfigurationHasDuplicateKeyIdentifiers(RijndaelEncryptionServiceConfig section)
        {
            // Combine all key identifier values, filter the empty ones, split them 
            return section
                .ExpiredKeys
                .Cast<RijndaelExpiredKey>()
                .Select(x => x.KeyIdentifier)
                .Union(new[]
                {
                    section.KeyIdentifier
                })
                .Where(x => !string.IsNullOrEmpty(x))
                .Select(x => x.Split(';'))
                .SelectMany(x => x)
                .GroupBy(x => x)
                .Any(x => x.Count() > 1);
        }

        public static bool ExpiredKeysHaveDuplicateKeys(RijndaelEncryptionServiceConfig section)
        {
            var items = section
                .ExpiredKeys
                .Cast<RijndaelExpiredKey>()
                .ToList();

            return items.Count != items.Select(x => x.Key).Distinct().Count();
        }

        public static bool EncryptionKeyListedInExpiredKeys(RijndaelEncryptionServiceConfig section)
        {
            return section
                .ExpiredKeys
                .Cast<RijndaelExpiredKey>()
                .Any(x => x.Key == section.Key);
        }

        public static bool OneOrMoreExpiredKeysHaveNoKeyIdentifier(RijndaelEncryptionServiceConfig section)
        {
            return section
                .ExpiredKeys
                .Cast<RijndaelExpiredKey>()
                .Any(x => string.IsNullOrEmpty(x.KeyIdentifier));
        }

        public static bool ExpiredKeysHaveWhiteSpace(RijndaelEncryptionServiceConfig section)
        {
            return section
                .ExpiredKeys
                .Cast<RijndaelExpiredKey>()
                .Any(x => string.IsNullOrWhiteSpace(x.Key));
        }
    }
}