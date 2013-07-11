namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using System.Collections.Generic;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.ObjectBuilder.Autofac;
    using NServiceBus.ObjectBuilder.CastleWindsor;
    using NServiceBus.ObjectBuilder.Ninject;
    using NServiceBus.ObjectBuilder.Spring;
    using NServiceBus.ObjectBuilder.StructureMap;
    using NServiceBus.ObjectBuilder.Unity;

    public static class Builders
    {
        public static readonly RunDescriptor Unity = new RunDescriptor
            {
                Key = "Unity",
                Settings =
                    new Dictionary<string, string>
                        {
                            {
                                "Builder", typeof (UnityObjectBuilder).AssemblyQualifiedName
                            }
                        }
            };

        public static readonly RunDescriptor Autofac = new RunDescriptor
            {
                Key = "Autofac",
                Settings =
                    new Dictionary<string, string>
                        {
                            {
                                "Builder", typeof (AutofacObjectBuilder).AssemblyQualifiedName
                            }
                        }
            };

        public static readonly RunDescriptor Windsor = new RunDescriptor
            {
                Key = "Windsor",
                Settings =
                    new Dictionary<string, string>
                        {
                            {
                                "Builder", typeof (WindsorObjectBuilder).AssemblyQualifiedName
                            }
                        }
            };

        public static readonly RunDescriptor Ninject = new RunDescriptor
            {
                Key = "Ninject",
                Settings =
                    new Dictionary<string, string>
                        {
                            {
                                "Builder", typeof (NinjectObjectBuilder).AssemblyQualifiedName
                            }
                        }
            };


        public static readonly RunDescriptor Spring = new RunDescriptor
            {
                Key = "Spring",
                Settings =
                    new Dictionary<string, string>
                        {
                            {
                                "Builder", typeof (SpringObjectBuilder).AssemblyQualifiedName
                            }
                        }
            };

        public static readonly RunDescriptor StructureMap = new RunDescriptor
            {
                Key = "StructureMap",
                Settings =
                    new Dictionary<string, string>
                        {
                            {
                                "Builder", typeof (StructureMapObjectBuilder).AssemblyQualifiedName
                            }
                        }
            };
    }
}