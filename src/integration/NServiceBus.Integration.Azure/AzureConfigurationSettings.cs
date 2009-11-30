using System;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace NServiceBus.Integration.Azure
{
    public class AzureConfigurationSettings : IAzureConfigurationSettings
    {
        public string GetSetting(string name)
        {
            if (!RoleEnvironment.IsAvailable)
                return "";

            //hack: the azure runtime throws if a setting doesn't exists and there is no way of 
            //checking that a setting is defined. Therefor we have to do this ugly stuff
            try
            {
                return RoleEnvironment.GetConfigurationSettingValue(name);
            }
            catch (Exception)
            {
                return "";
            }
            
        }
    }
}