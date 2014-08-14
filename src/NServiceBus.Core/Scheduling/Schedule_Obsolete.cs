#pragma warning disable 1591
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    public partial class Schedule
    {
        [ObsoleteEx(
            TreatAsErrorFromVersion = "5.0", 
            RemoveInVersion = "6.0", 
            Replacement = "The non-static version of Schedule.Every()", 
            Message = "Inject an instance of Schedule to your class and then call the non static members.")]
        public static Schedule Every(TimeSpan timeSpan)
        {
            throw new NotImplementedException("Api has been obsolete.");
        }

        [ObsoleteEx(TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0", Replacement = "Schedule.Every(TimeSpan timeSpan, Action task)")]
        public void Action(Action task)
        {
            throw new NotImplementedException("Api has been obsolete.");
        }

        [ObsoleteEx(TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0", Replacement = "Schedule.Every(TimeSpan timeSpan, string name, Action task)")]
        public void Action(string name, Action task)
        {
            throw new NotImplementedException("Api has been obsolete.");
        }

    }
}
