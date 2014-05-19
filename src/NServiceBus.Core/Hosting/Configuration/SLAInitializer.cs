namespace NServiceBus
{
    using System;
    using System.Linq;

    class SLAInitializer : INeedInitialization
    {
        public IConfigureThisEndpoint Config { get; set; }
     
        public void Init(Configure config)
        {
            var configType = config.TypesToScan.SingleOrDefault(t => typeof(IConfigureThisEndpoint).IsAssignableFrom(t) && !t.IsInterface);

            if (configType == null)
            {
                return;
            }

            var arr = configType.GetCustomAttributes(typeof(EndpointSLAAttribute), false);
            if (arr.Length != 1)
            {
                return;
            }
                

            var slaString = ((EndpointSLAAttribute)arr.First()).SLA;

            TimeSpan sla;

            if (!TimeSpan.TryParse(slaString, out sla))
                throw new InvalidOperationException("A invalid SLA string has been defined - " + slaString);

            config.SetEndpointSLA(sla);
        }
    }
}