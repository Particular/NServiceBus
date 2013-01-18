namespace NServiceBus.IntegrationTests.Automated.Support
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Installation.Environments;

    [Serializable]
    public class EndpointRunner:MarshalByRefObject
    {
        IStartableBus startableBus;
        IBus bus;

        EndpointScenario scenario;


        public bool Initialize(string scenarioType)
        {
            scenario = ((IScenarioFactory)Activator.CreateInstance(Type.GetType(scenarioType))).Get();



            var config = Configure.With()
                .DefineEndpointName(scenario.EndpointName)
                .CustomConfigurationSource(new ScenarioConfigSource(scenario));

            scenario.SetupActions.ForEach(setup=> setup(config));           
            
            startableBus = config.CreateBus();

            Configure.Instance.ForInstallationOn<Windows>().Install();


            return true;
        }

       
        public bool Start()
        {
            bus = startableBus.Start();

            scenario.Givens.ForEach(a=>a(bus));

            return true;

        }


        public void ApplyWhens()
        {
            scenario.Whens.ForEach(a => a(bus));
        }

        public string Name()
        {
            return AppDomain.CurrentDomain.FriendlyName;
        }

      
        public bool Done()
        {
            return scenario.Done();
        }

        public string[] VerifyAssertions()
        {
            var failures = new List<string>();
            foreach (var assertion in scenario.Assertions)
            {
                var failure = "";
                try
                {
                    assertion();
                }
                catch (Exception ex)
                {
                    failure = ex.Message;
                }

                if(!string.IsNullOrEmpty(failure))
                    failures.Add(failure);
            }

            return failures.ToArray();
        }
    }
}