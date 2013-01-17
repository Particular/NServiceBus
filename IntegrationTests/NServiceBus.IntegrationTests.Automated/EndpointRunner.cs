namespace NServiceBus.IntegrationTests.Automated
{
    using System;
    using System.Collections.Generic;
    using Installation.Environments;

    [Serializable]
    public class EndpointRunner:MarshalByRefObject
    {
        IStartableBus startableBus;
        IBus bus;

        EndpointScenario scenario;


        public bool Initialize(string scenarioType)
        {
            Console.Out.WriteLine("Initialize from " + AppDomain.CurrentDomain.FriendlyName);

            scenario = ((IScenarioFactory)Activator.CreateInstance(Type.GetType(scenarioType))).Get();



            startableBus = Configure.With()
                .DefineEndpointName(scenario.EndpointName)
                .CustomConfigurationSource(new ScenarioConfigSource(scenario))
                                    .DefaultBuilder()
                                    .XmlSerializer()
                                    .MsmqTransport()
                                    .UnicastBus()
                                    .CreateBus();

            Configure.Instance.ForInstallationOn<Windows>().Install();


            return true;
        }

       
        public bool Start()
        {
            Console.Out.WriteLine("Start from " + AppDomain.CurrentDomain.FriendlyName);

            bus = startableBus.Start();

            foreach (var action in scenario.InitialBusActions)
                action(bus);

            return true;

        }

        public string Name()
        {
            return AppDomain.CurrentDomain.FriendlyName;
        }

      
        public bool Done()
        {
            return scenario.Done();
        }

        public IEnumerable<string> FailedAssertions()
        {
            return new List<string>();
        }
    }
}