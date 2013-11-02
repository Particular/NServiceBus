namespace NServiceBus
{
    using System;
    using System.Linq;

    class SLAInitializer : IWantCustomInitialization,IWantTheEndpointConfig
    {
        public void Init()
        {
            var arr = Config.GetType().GetCustomAttributes(typeof(EndpointSLAAttribute), false);
            if (arr.Length != 1)
                return;


            var slaString = (arr.First() as EndpointSLAAttribute).SLA;
            
            TimeSpan sla;

            if (!TimeSpan.TryParse(slaString, out sla))
                throw new InvalidOperationException("A invalid SLA string has been defined - " + slaString);

            Configure.Instance.SetEndpointSLA(sla);
        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}