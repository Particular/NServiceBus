namespace NServiceBus.Pipeline
{
    using System;
    using System.Collections.Generic;

    class Satellite
    {
        readonly HashSet<Type> enabledFeatures = new HashSet<Type>();
        readonly string name;
        readonly string receiveAddress;
        readonly PipelineModificationsBuilder specificFeaturesRegistration = new PipelineModificationsBuilder();

        public Satellite(string name, string receiveAddress, RegisterStep pipelineBehavior)
        {
            this.name = name;
            this.receiveAddress = receiveAddress;
            specificFeaturesRegistration.AddAddition(pipelineBehavior);
            new PipelineSettings(specificFeaturesRegistration).RegisterConnector<TransportReceiveToPhysicalMessageProcessingConnector>("Allows to abort processing the message");
        }

        public string Name
        {
            get { return name; }
        }

        public string ReceiveAddress
        {
            get { return receiveAddress; }
        }

        public PipelineModificationsBuilder SpecificFeaturesRegistration
        {
            get { return specificFeaturesRegistration; }
        }

        public void EnableFeature(Type featureType)
        {
            enabledFeatures.Add(featureType);
        }

        public bool IsEnabled(Type featureType)
        {
            return enabledFeatures.Contains(featureType);
        }
    }
}