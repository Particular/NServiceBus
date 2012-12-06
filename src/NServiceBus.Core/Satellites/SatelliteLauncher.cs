namespace NServiceBus.Satellites
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
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
            timer = new System.Timers.Timer {Interval = 1000};
            timer.Elapsed += (o, e) => StartSatellites();

            Build();
            Initialize();
            StartSatellites();
        }

        public void Stop()
        {
            foreach (var ctx in AllSatellitesThatShouldBeStarted())
            {
                if (ctx.Transport != null)
                {
                    ctx.Transport.ChangeNumberOfWorkerThreads(0);
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
            timer.Stop();

            foreach (var ctx in AllSatellitesThatShouldBeStarted())
            {
                ctx.Started = true;
                StartSatellite(ctx);
            }
            
            timer.Start();
        }        
        
        IEnumerable<SatelliteContext> AllSatellitesThatShouldBeStarted()
        {
            return satellites.Where(sat => 
                sat.Instance != null && 
                !sat.Instance.Disabled && 
                !sat.Started &&                 
                sat.FailedAttempts < 3);
        }

        protected virtual void StartSatellite(SatelliteContext ctx)
        {
            var thread = new Thread(Execute) {IsBackground = true};
            thread.Start(ctx);
        }

        void HandleMessageReceived(object sender, TransportMessageReceivedEventArgs e, ISatellite satellite)
        {
            try
            {
                satellite.Handle(e.Message);
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("{0} could not handle message. Exception: {1}", satellite.GetType().Name, ex.Message);
                throw;
            }            
        }
                
        protected void Execute(object data)
        {
            SatelliteContext ctx = null;
            try
            {
                ctx = (SatelliteContext)data;
                if (ctx.Transport != null)
                {
                    ctx.Transport.TransportMessageReceived += (o, e) => HandleMessageReceived(o, e, ctx.Instance);
                    ctx.Transport.Start(ctx.Instance.InputAddress);

                    Logger.DebugFormat("Starting transport {0} for satellite {1} using {2} thread(s)", ctx.Instance.InputAddress, ctx.Instance.GetType().Name, ctx.Transport.NumberOfWorkerThreads);
                }
                else
                {
                    Logger.DebugFormat("No input queue configured for {0}", ctx.Instance.GetType().Name);
                }
                ctx.Instance.Start();
            }
            catch (Exception ex)
            {
                if (ctx != null)
                {
                    Logger.WarnFormat("Satellite {0} failed because of {1}", ctx.Instance.GetType().Name, ex.Message);

                    ctx.Started = false;

                    if (ctx.Transport != null)
                    {                        
                        ctx.Transport.ChangeNumberOfWorkerThreads(0);                        
                    }

                    ctx.FailedAttempts++;
                }
            }
        }
     
        static readonly ILog Logger = LogManager.GetLogger("SatelliteLauncher");

        System.Timers.Timer timer;

        private readonly ConcurrentBag<SatelliteContext> satellites = new ConcurrentBag<SatelliteContext>();
    }

    public class SatelliteArgs : EventArgs
    {
        public ISatellite Satellite { get; set; }
        public ITransport Transport { get; set; }
    }
}