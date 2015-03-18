namespace NServiceBus
{
    using System;
    using NServiceBus.Hosting;
    using NServiceBus.Pipeline;

    class HostInformationBehavior : PhysicalMessageProcessingStageBehavior
    {
        readonly HostInformation hostInfo;

        public HostInformationBehavior(HostInformation hostInfo)
        {
            this.hostInfo = hostInfo;
        }

        public override void Invoke(Context context, Action next)
        {
            context.PhysicalMessage.Headers[Headers.HostId] = hostInfo.HostId.ToString("N");
            context.PhysicalMessage.Headers[Headers.HostDisplayName] = hostInfo.DisplayName;

            next();
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("AddHostInformation", typeof(HostInformationBehavior), "Adds host information")
            {
            }
        }
    }
}