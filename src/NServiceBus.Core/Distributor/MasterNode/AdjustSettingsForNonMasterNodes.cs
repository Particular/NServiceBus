namespace NServiceBus.MasterNode
{
    using Settings;

    class AdjustSettingsForNonMasterNodes : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run()
        {
            if (!Configure.Instance.HasMasterNode())
                return;

            SettingsHolder.SetDefault("SecondLevelRetries.AddressOfRetryProcessor", Configure.Instance.GetMasterNodeAddress().SubScope("Retries"));
        }
    }
}