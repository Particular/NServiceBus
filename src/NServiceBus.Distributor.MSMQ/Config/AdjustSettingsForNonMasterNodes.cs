namespace NServiceBus.Distributor.MSMQ.Config
{
    using Settings;

    class AdjustSettingsForNonMasterNodes : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run()
        {
            if (!MasterNodeUtils.HasMasterNode())
            {
                return;
            }

            SettingsHolder.SetDefault("SecondLevelRetries.AddressOfRetryProcessor", MasterNodeUtils.GetMasterNodeAddress().SubScope("Retries"));
        }
    }
}