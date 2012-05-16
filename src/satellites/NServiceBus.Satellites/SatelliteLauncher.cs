using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using log4net;
using NServiceBus.ObjectBuilder;
using NServiceBus.Satellites.Config;
using NServiceBus.Unicast;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Satellites
{
    public class SatelliteLauncher : IWantToRunWhenTheBusStarts
    {
        ILog Logger = LogManager.GetLogger("SatelliteLauncher");

        System.Timers.Timer timer;
        internal static ConcurrentBag<SatelliteContext> Satellites = new ConcurrentBag<SatelliteContext>();
        
        public IBuilder Builder { get; set; }        
        public ISatelliteTransportBuilder TransportBuilder { get; set; }
        
        public void Run()
        {
            timer = new System.Timers.Timer {Interval = 1000};
            timer.Elapsed += (o, e) => Start();
            
            Initialize();
            Start();
        }

        void Initialize()
        {
            foreach (var ctx in Satellites.Where(c => c.Enabled))
            {
                Logger.DebugFormat("Initializing satellite {0}", ctx.TypeOfSatellite.Name);
                ctx.Instance = Builder.Build(ctx.TypeOfSatellite) as ISatellite;

                if (ctx.Instance == null)
                {
                    Logger.DebugFormat("Could not build satellite {0}", ctx.TypeOfSatellite.Name);
                    ctx.Enabled = false;
                    continue;
                }

                if (ctx.Instance.InputAddress != null && ctx.Instance.Disabled == false)
                {                    
                    ctx.Transport = TransportBuilder.Build(ctx.NumberOfWorkerThreads, ctx.MaxRetries, ctx.IsTransactional);
                }                
            }
        }
        
        void Start()
        {
            timer.Stop();

            foreach (var ctx in AllSatellitesThatShouldBeStarted())
            {
                ctx.Started = true;
                StartSatellite(ctx);
            }
            
            timer.Start();
        }        
        
        static IEnumerable<SatelliteContext> AllSatellitesThatShouldBeStarted()
        {
            return Satellites.Where(sat => 
                sat.Instance != null && 
                !sat.Instance.Disabled && 
                !sat.Started && 
                sat.Enabled &&
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

                    // if exception is thrown, worker threads are set to 0. That's why we here increase them again.
                    if (ctx.NumberOfWorkerThreads != ctx.Transport.NumberOfWorkerThreads && ctx.FailedAttempts > 0)
                    {
                        ctx.Transport.ChangeNumberOfWorkerThreads(ctx.NumberOfWorkerThreads);
                    }

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
                        //ctx.Transport.Stop() ?
                        ctx.Transport.ChangeNumberOfWorkerThreads(0);                        
                    }

                    ctx.FailedAttempts++;
                }
            }
        }
    }
}