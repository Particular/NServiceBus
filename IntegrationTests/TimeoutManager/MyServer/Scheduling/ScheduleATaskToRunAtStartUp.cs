using System;
using System.Threading;
using NServiceBus;
using NServiceBus.Unicast;

namespace MyServer.Scheduling
{
    public class ScheduleATaskToRunAtStartUp //: IWantToRunWhenTheBusStarts 
    {
        public void Run()
        {
            Schedule.Every(TimeSpan.FromMinutes(5)).Action(() => Console.WriteLine("This task was schduled when the host started"));
            Schedule.Every(TimeSpan.FromMinutes(3)).Action("Task with specified name",() =>
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