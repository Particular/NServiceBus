namespace Particular.Licensing
{
    using System;
    using System.Globalization;
    using Microsoft.Win32;

    static class TrialStartDateStore
    {
        public static DateTime GetTrialStartDate()
        {
            var rootKey = Registry.LocalMachine;

            if (UserSidChecker.IsNotSystemSid())
            {
                rootKey = Registry.CurrentUser;
            }
            using (var registryKey = rootKey.CreateSubKey(StorageLocation))
            {
                // ReSharper disable PossibleNullReferenceException
                //CreateSubKey does not return null http://stackoverflow.com/questions/19849870/under-what-circumstances-will-registrykey-createsubkeystring-return-null
                var trialStartDateString = (string)registryKey.GetValue("TrialStart", null);
                // ReSharper restore PossibleNullReferenceException
                if (trialStartDateString == null)
                {
                    var trialStart = DateTime.UtcNow;
                    trialStartDateString = trialStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    registryKey.SetValue("TrialStart", trialStartDateString, RegistryValueKind.String);

                    return trialStart;
                }

                var trialStartDate = DateTimeOffset.ParseExact(trialStartDateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

                return trialStartDate.Date;
            }
        }

        public static string StorageLocation = @"SOFTWARE\ParticularSoftware";
    }
}