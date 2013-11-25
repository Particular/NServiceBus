namespace NServiceBus.Licensing
{
    using System;
    using System.Globalization;
    using Logging;
    using Microsoft.Win32;

    static class TrialLicenseReader
    {
        internal const int TRIAL_DAYS = 14;
        static ILog Logger = LogManager.GetLogger(typeof(TrialLicenseReader));

        public static DateTime GetTrialExpirationFromRegistry()
        {
            //If first time run, configure expire date
            try
            {
                var subKeyPath = String.Format(@"SOFTWARE\NServiceBus\{0}", NServiceBusVersion.MajorAndMinor);
                using (var registryKey = Registry.CurrentUser.CreateSubKey(subKeyPath))
                {
                    //CreateSubKey does not return null http://stackoverflow.com/questions/19849870/under-what-circumstances-will-registrykey-createsubkeystring-return-null
                    // ReSharper disable once PossibleNullReferenceException
                    var trialStartDateString = (string) registryKey.GetValue("TrialStart", null);
                    if (trialStartDateString == null)
                    {
                        var trialStart = DateTime.UtcNow;
                        trialStartDateString = trialStart.ToString("yyyy-MM-dd");
                        registryKey.SetValue("TrialStart", trialStartDateString, RegistryValueKind.String);

                        Logger.DebugFormat("First time running NServiceBus v{0}, setting trial license start.", NServiceBusVersion.MajorAndMinor);
                        return trialStart.AddDays(TRIAL_DAYS);
                    }
                    else
                    {
                        var trialStartDate = DateTimeOffset.ParseExact(trialStartDateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

                        return trialStartDate.Date.AddDays(TRIAL_DAYS);
                        
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Debug("Could not access registry to check trial expiration date. Because we didn't find a license file we assume the trial has expired.", ex);
                return DateTime.MinValue;
            }
        }
    }

}