namespace NServiceBus.Distributor
{
    class EndpointRunner : IWantToRunAtStartup
    {
        public Unicast.Distributor.Distributor Distributor { get; set; }

        public void Run()
        {
            Distributor.Start();
        }

        public void Stop()
        {
            Distributor.Stop();
        }
    }
}
