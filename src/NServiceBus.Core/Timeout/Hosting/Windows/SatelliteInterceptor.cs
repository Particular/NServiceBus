namespace NServiceBus.Timeout.Hosting.Windows
{
    using NServiceBus.Config;
    using Satellites;
    using Unicast.Transport.Transactional;

    public class SatelliteInterceptor : IWantToRunWhenConfigurationIsComplete
    {
        public void Run()
        {
            SatelliteLauncher.SatelliteTransportInitialized += SatelliteLauncherOnSatelliteTransportInitialized;
        }

        private void SatelliteLauncherOnSatelliteTransportInitialized(object sender, SatelliteArgs args)
        {
            if (args.Satellite is TimeoutMessageProcessor || args.Satellite is TimeoutDispatcherProcessor)
            {
                //TODO: The line below needs to change when we refactore the slr to be:
                // transport.DisableSLR() or similar
                var transactionalTransport = ((TransactionalTransport) args.Transport);
                transactionalTransport.FailureManager = new ManageMessageFailuresWithoutSlr(transactionalTransport.FailureManager);
            }
        }
    }
}