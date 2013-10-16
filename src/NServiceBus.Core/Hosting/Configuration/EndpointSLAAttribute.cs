namespace NServiceBus
{
    using System;
    using System.Linq;

    /// <summary>
    /// Defines the SLA for this endpoint. Needs to be set on the endpoint configuration class
    /// </summary>
    public class EndpointSLAAttribute : Attribute
    {
        /// <summary>
        /// Used to define the SLA for this endpoint
        /// </summary>
        /// <param name="sla">A <see cref="string"/> representing a <see cref="TimeSpan"/></param>
        public EndpointSLAAttribute(string sla)
        {
            SLA = sla;
        }

        /// <summary>
        /// The SLA of the endpoint.
        /// </summary>
        public string SLA { get; set; }
    }

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