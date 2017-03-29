namespace Particular.Licensing
{
    using System.Linq;

    class LicenseSourceResult
    {
        internal License License { get; set; }

        internal string Location { get; set; }

        internal string Result { get; set; }

        public static LicenseSourceResult DetermineBestLicenseSourceResult(params LicenseSourceResult[] sourceResults)
        {
            if (sourceResults.All(p => p.License == null))
            {
                return null;
            }
            var sourcesResultsWithLicenseOrderedByDate = sourceResults.Where(p => p.License != null).OrderByDescending(p => p.License.ExpirationDate).ToList();

            // Can't rely on just expiry date as running on a build that was produced after the upgrade protection expiration is the same as unlicensed.
            var unexpiredResult = sourcesResultsWithLicenseOrderedByDate.FirstOrDefault(p => !LicenseExpirationChecker.HasLicenseExpired(p.License));

            return unexpiredResult ?? sourcesResultsWithLicenseOrderedByDate.First();
        }
    }
}
