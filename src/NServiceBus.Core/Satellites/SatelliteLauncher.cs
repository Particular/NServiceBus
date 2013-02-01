namespace NServiceBus.Satellites
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
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
            Build();
            Initialize();
            StartSatellites();
        }

        public void Stop()
        {
            foreach (var ctx in satellites.Where(ctx => ctx.Started))
            {
                Logger.DebugFormat("Stoping satellite {0}.", ctx.Instance.GetType().AssemblyQualifiedName);

                if (ctx.Transport != null)
                {
                    ctx.Transport.Dispose();
                }

                ctx.Instance.Stop();
            }
        }

        void Build()
        {
            Configure.Instance.Builder
                .BuildAll<ISatellite>()
                .ToList()
                .ForEach(s => satellites.Add(new SatelliteContext
                                                 {
                                                     Instance = s
                                                 }));
        }

        void Initialize()
        {
            foreach (var ctx in satellites)
            {                                
                if (ctx.Instance == null)
                {
                    Logger.DebugFormat("No satellite found for context!");                    
                    continue;
                }

                if (ctx.Instance.InputAddress != null && ctx.Instance.Disabled == false)
                {
                    ctx.Transport = Builder.Build<ITransport>();

                    if (SatelliteTransportInitialized != null)
                    {
                        SatelliteTransportInitialized(null,
                                                      new SatelliteArgs
                                                          {
                                                              Satellite = ctx.Instance,
                                                              Transport = ctx.Transport
                                                          });
                    }
                }                
            }
        }

        void StartSatellites()
        {
            var allSatellitesThatShouldBeStarted = AllSatellitesThatShouldBeStarted();

            if (!allSatellitesThatShouldBeStarted.Any())
            {
                return;
            }

            foreach (var ctx in allSatellitesThatShouldBeStarted)
            {
                StartSatellite(ctx);
            }
        }        
        
        IList<SatelliteContext> AllSatellitesThatShouldBeStarted()
        {
            return satellites
                .Where(sat => sat.Instance != null && !sat.Instance.Disabled)
                .ToList();
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

                ctx.Started = false;

                if (ctx.Transport != null)
                {
                    ctx.Transport.ChangeMaximumConcurrencyLevel(0);                        
                }
            }
        }
     
        static readonly ILog Logger = LogManager.GetLogger("SatelliteLauncher");

        private readonly ConcurrentBag<SatelliteContext> satellites = new ConcurrentBag<SatelliteContext>();
    }

    public class SatelliteArgs : EventArgs
    {
        public ISatellite Satellite { get; set; }
        public ITransport Transport { get; set; }
    }
}