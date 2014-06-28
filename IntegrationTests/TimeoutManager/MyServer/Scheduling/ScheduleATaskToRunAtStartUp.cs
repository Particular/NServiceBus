using System;
using System.Threading;
using NServiceBus;

namespace MyServer.Scheduling
{
    public class ScheduleATaskToRunAtStartUp : IWantToRunWhenBusStartsAndStops 
    {
        public Schedule Schedule { get; set; }
        public void Start()
        {
            Schedule.Every(TimeSpan.FromMinutes(5),() => Console.WriteLine("This task was schduled when the host started"));

            Schedule.Every(TimeSpan.FromMinutes(3),"Task with specified name",() =>
                                                                                          {
                                                                                              Thread.Sleep(60 * 1000);
                                                                                              Console.WriteLine("This task was schduled when the host started and given a name");
                                                                                          });
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}