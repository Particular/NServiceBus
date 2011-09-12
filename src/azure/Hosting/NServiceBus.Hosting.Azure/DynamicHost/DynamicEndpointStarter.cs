using System;
using System.Collections.Generic;
using System.Diagnostics;
using Common.Logging;

namespace NServiceBus.Hosting
{
    internal class DynamicEndpointStarter
    {
        private readonly ILog logger = LogManager.GetLogger(typeof(DynamicEndpointStarter));

        public void Start(IEnumerable<ServiceToRun> toHost)
        {
            foreach(var service in toHost)
            {
                try
                {
                    var processStartInfo = new ProcessStartInfo(service.EntryPoint,
                                                               "/serviceName:\"" + service.ServiceName +
                                                                "\" /displayName:\"" + service.ServiceName +
                                                                "\" /description:\"" + service.ServiceName + "\"")
                                               {
                                                   UseShellExecute = false,
                                                   CreateNoWindow = true,
                                                   RedirectStandardInput = true,
                                                   RedirectStandardOutput = true
                                               };



                    var process = new Process {StartInfo = processStartInfo, EnableRaisingEvents = true};


                    process.ErrorDataReceived += (o, args) => logger.Error(args.Data);
                    process.Exited += (o, args) =>
                                          {
                                              var output = process.StandardOutput.ReadToEnd();
                                              if (process.ExitCode == -1) 
                                                  logger.Error(output);
                                              else 
                                                  logger.Debug(output);
                                          };

                    process.Start();
                }
                catch (Exception e)
                {
                    logger.Error(e.Message);
                }

            }
           
        }

      
    }
}