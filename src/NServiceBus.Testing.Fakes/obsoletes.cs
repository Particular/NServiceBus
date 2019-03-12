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
            throw new NotSupportedException("HandleCurrentMessageLater has been deprecated and will be removed in NServiceBus.Core Version 8.");
        }
        
        [ObsoleteEx(
            Message = "HandleCurrentMessageLaterWasCalled has been deprecated.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public bool HandleCurrentMessageLaterWasCalled => throw new NotSupportedException("HandleCurrentMessageLater has been deprecated and will be removed in NServiceBus.Core Version 8.");
    }
}