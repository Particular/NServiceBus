namespace NServiceBus
{
    public static class ConfigureTransport
    {
        /// <summary>
        /// Sets the max throughput for the transport
        /// </summary>
        /// <param name="config"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Configure LimitThroughputTo(this Configure config,int value)
        {
            Unicast.Transport.Transactional.Config.Bootstrapper.MaxThroughput = value;

            return config;
        }
    }
}