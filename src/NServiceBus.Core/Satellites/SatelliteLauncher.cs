namespace NServiceBus.Satellites
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Config;
    using Logging;
    using ObjectBuilder;
    using Unicast.Transport;

    public class SatelliteLauncher : IWantToRunWhenBusStartsAndStops
    {
        public IBuilder Builder { get; set; }

        public void Start()
        {
            var satellitesList = Configure.Instance.Builder
                                          .BuildAll<ISatellite>()
                                          .ToList()
                                          .Where(s => !s.Disabled)
                                          .ToList();

            var satelliteContexts = new SatelliteContext[satellitesList.Count];

            Parallel.For(0, satellitesList.Count, index =>
                {
                    var satellite = satellitesList[index];

                    Logger.DebugFormat("Starting {1}/{2} '{0}' satellite", satellite.GetType().AssemblyQualifiedName,
                                       index + 1, satellitesList.Count);

                    var ctx = new SatelliteContext
                        {
                            Instance = satellite
                        };

                    if (satellite.InputAddress != null)
                    {
                        ctx.Transport = Builder.Build<TransportReceiver>();

                        var advancedSatellite = satellite as IAdvancedSatellite;
                        if (advancedSatellite != null)
                        {
                            var receiverCustomization = advancedSatellite.GetReceiverCustomization();

                            receiverCustomization(ctx.Transport);
                        }
                    }

                    StartSatellite(ctx);

                    satelliteContexts[index] = ctx;

                    Logger.InfoFormat("Started {1}/{2} '{0}' satellite", satellite.GetType().AssemblyQualifiedName,
                                       index + 1, satellitesList.Count);

                });

            satellites.AddRange(satelliteContexts);
        }

        public void Stop()
        {
            Parallel.ForEach(satellites, (ctx, state, index) =>
                {
                    Logger.DebugFormat("Stopping {1}/{2} '{0}' satellite", ctx.Instance.GetType().AssemblyQualifiedName,
                                       index + 1, satellites.Count);

                    if (ctx.Transport != null)
                    {
                        ctx.Transport.Stop();
                    }

                    ctx.Instance.Stop();

                    Logger.InfoFormat("Stopped {1}/{2} '{0}' satellite", ctx.Instance.GetType().AssemblyQualifiedName,
                                       index + 1, satellites.Count);
                });
        }

        void HandleMessageReceived(object sender, TransportMessageReceivedEventArgs e, ISatellite satellite)
        {
            if (!satellite.Handle(e.Message))
            {
                ((ITransport) sender).AbortHandlingCurrentMessage();
            }
        }

        void StartSatellite(SatelliteContext ctx)
        {
            Logger.DebugFormat("Starting satellite {0} for {1}.", ctx.Instance.GetType().AssemblyQualifiedName,
                               ctx.Instance.InputAddress);

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
                Logger.Error(
                    string.Format("Satellite {0} failed to start.", ctx.Instance.GetType().AssemblyQualifiedName), ex);

                if (ctx.Transport != null)
                {
                    ctx.Transport.ChangeMaximumConcurrencyLevel(0);
                }
            }
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof (SatelliteLauncher));

        readonly List<SatelliteContext> satellites = new List<SatelliteContext>();
    }
}