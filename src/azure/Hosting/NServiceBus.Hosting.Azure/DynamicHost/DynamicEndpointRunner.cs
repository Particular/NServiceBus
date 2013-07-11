using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NServiceBus.Logging;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace NServiceBus.Hosting
{
    internal class DynamicEndpointRunner
    {
        private readonly ILog logger = LogManager.GetLogger(typeof(DynamicEndpointRunner));

        public bool RecycleRoleOnError { get; set; }

        public int TimeToWaitUntilProcessIsKilled { get; set; }

        public void Start(IEnumerable<EndpointToHost> toHost)
        {
            foreach(var service in toHost)
            {
                try
                {
                    var processStartInfo = new ProcessStartInfo(service.EntryPoint,
                                                               "/serviceName:\"" + service.EndpointName +
                                                                "\" /displayName:\"" + service.EndpointName +
                                                                "\" /description:\"" + service.EndpointName + "\"")
                                               {
                                                   UseShellExecute = false,
                                                   CreateNoWindow = true,
                                                   RedirectStandardInput = true,
                                                   RedirectStandardOutput = true
                                               };
                    
                    var process = new Process {StartInfo = processStartInfo, EnableRaisingEvents = true};
                    
                    process.ErrorDataReceived += (o, args) =>
                                                     {
                                                         logger.Error(args.Data);

                                                         if (RecycleRoleOnError) RoleEnvironment.RequestRecycle();
                                                     };
                    process.Exited += (o, args) =>
                                          {
                                              var output = process.StandardOutput.ReadToEnd();
                                              if (process.ExitCode != 0)
                                              {
                                                  logger.Error(output);
                                                  if (RecycleRoleOnError) RoleEnvironment.RequestRecycle();
                                              }
                                              else
                                              {
                                                  logger.Debug(output);
                                              }
                                          };

                    process.Start();

                    service.ProcessId = process.Id;
                    
                }
                catch (Exception e)
                {
                    logger.Error(e.Message);

                    if (RecycleRoleOnError) RoleEnvironment.RequestRecycle();
                }
            }
        }

        public void Stop(IEnumerable<EndpointToHost> runningServices)
        {
            foreach (var runningService in runningServices)
            {
                if (runningService.ProcessId == 0) continue;

                var process = Process.GetProcessById(runningService.ProcessId);
                
                var waitUntilProcessIsKilled = new ManualResetEvent(false);
                process.Exited += (o, args) => waitUntilProcessIsKilled.Set();
                   
                process.Kill();
                waitUntilProcessIsKilled.WaitOne(TimeToWaitUntilProcessIsKilled);
                if(!process.HasExited)
                {
                    throw new UnableToKillProcessException(string.Format("Unable to kill process {0}",  process.ProcessName));
                }
                runningService.ProcessId = 0;
            }
        }

        
    }
}