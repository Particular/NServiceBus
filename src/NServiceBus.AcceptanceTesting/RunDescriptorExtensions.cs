namespace NServiceBus.AcceptanceTesting
{
    using System;
    using NServiceBus.AcceptanceTesting.Support;

    public static class RunDescriptorExtensions
    {
        public static Type GetTransportType(this RunDescriptor runDescriptor)
        {
            return Type.GetType(runDescriptor.Settings["Transport"]);
        }
    }
}