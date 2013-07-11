namespace NServiceBus.Integration.Azure
{
    using System;
    using Microsoft.WindowsAzure.ServiceRuntime;
    //using Microsoft.WindowsAzure.ServiceRuntime.Internal;

    public class AzureConfigurationSettings : IAzureConfigurationSettings
    {
        public string GetSetting(string name)
        {
            if (!RoleEnvironment.IsAvailable)
                return "";

            return TryGetSetting(name);
        }

        static string TryGetSetting(string name)
        {
            try
            {
                return RoleEnvironment.GetConfigurationSettingValue(name);
            }
            catch (Exception)
            {
                return "";
            }

            // code left in comments here, until build server has sdk installed

            //hack: the azure runtime throws if a setting doesn't exists and this seems to be a way of 
            //checking that a setting is defined. Still ugly stuff though because of the dep on msshrtmi

            //string ret;
            //var hr = InteropRoleManager.GetConfigurationSetting(name, out ret);

            //if (HResult.Failed(hr))
            //{
            //    return "";
            //}

            //return ret;
        }
    }
}