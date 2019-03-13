#pragma warning disable 1591
namespace NServiceBus.Testing
{
    using System;
    using System.Threading.Tasks;
    
    public partial class TestableInvokeHandlerContext
    {        
        [ObsoleteEx(
            Message = "HandleCurrentMessageLater has been deprecated.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public Task HandleCurrentMessageLater()
        {
            throw new NotImplementedException();
        }
        
        [ObsoleteEx(
            Message = "HandleCurrentMessageLaterWasCalled has been deprecated.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public bool HandleCurrentMessageLaterWasCalled => throw new NotImplementedException();
    }
}
#pragma warning restore 1591