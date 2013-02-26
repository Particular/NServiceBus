namespace NServiceBus.Satellites
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Config;
    using Logging;
    using ObjectBuilder;
    using Unicast.Transport;

    public class SatelliteLauncher : IWantToRunWhenBusStartsAndStops
    {
        public static event EventHandler<SatelliteArgs> SatelliteTransportInitialized;

        public IBuilder Builder { get; set; }

        public void Start()
        {
            Configure.Instance.Builder
                .BuildAll<ISatellite>()
                .ToList()
                .ForEach(s =>
                {
                    if (s.Disabled)
                    {
                        return;
                    }

                    var ctx = new SatelliteContext
                    {
                        Instance = s
                    };

                    if (s.InputAddress != null)
                    {
                        ctx.Transport = Builder.Build<ITransport>();

                        if (SatelliteTransportInitialized != null)
                        {
                            SatelliteTransportInitialized(null,
                                                          new SatelliteArgs
                                                          {
                                                              Satellite = s,
                                                              Transport = ctx.Transport
                                                          });
                        }
                    }

                    StartSatellite(ctx);

                    satellites.Add(ctx);
                });
        }

        public void Stop()
        {
            foreach (var ctx in satellites)
            {
                Logger.DebugFormat("Stopping satellite {0}.", ctx.Instance.GetType().AssemblyQualifiedName);

                if (ctx.Transport != null)
                {
                    ctx.Transport.Stop();
                    ctx.Transport.Dispose();
                }

                ctx.Instance.Stop();
            }
        }

        void HandleMessageReceived(object sender, TransportMessageReceivedEventArgs e, ISatellite satellite)
        {
            try
            {
                if (!satellite.Handle(e.Message))
                {
                    ((ITransport)sender).AbortHandlingCurrentMessage();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("{0} satellite could not handle message.", satellite.GetType().AssemblyQualifiedName), ex);
                throw;
            }            
        }

        void StartSatellite(SatelliteContext ctx)
        {
            Logger.DebugFormat("Starting satellite {0} for {1}.", ctx.Instance.GetType().AssemblyQualifiedName, ctx.Instance.InputAddress);

            try
            {
                if (ctx.Transport != null)
                {
                    ctx.Transport.TransportMessageReceived += (o, e) => HandleMessageReceived(o, e, ctx.Instance);
                    ctx.Transport.Start(ctx.Instance.InputAddress);
                }
                else
                {
                    Logger.DebugFormat("No input queue configured for {0}", ctx.Instance.GetType().AssemblyQualifiedName);
                }

                ctx.Instance.Start();
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("Satellite {0} failed to start.", ctx.Instance.GetType().AssemblyQualifiedName), ex);

                if (ctx.Transport != null)
                {
                    ctx.Transport.ChangeMaximumConcurrencyLevel(0);                        
                }
            }
        }
     
        static readonly ILog Logger = LogManager.GetLogger("NServiceBus.SatelliteLauncher");

        private readonly List<SatelliteContext> satellites = new List<SatelliteContext>();
    }

    public class SatelliteArgs : EventArgs
    {
        public ISatellite Satellite { get; set; }
        public ITransport Transport { get; set; }
    }
}