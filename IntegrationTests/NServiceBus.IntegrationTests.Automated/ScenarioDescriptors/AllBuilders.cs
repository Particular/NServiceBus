namespace NServiceBus.IntegrationTests.Automated.ScenarioDescriptors
{
    using System.Collections.Generic;
    using ObjectBuilder.Autofac;
    using ObjectBuilder.CastleWindsor;
    using ObjectBuilder.Ninject;
    using ObjectBuilder.Spring;
    using ObjectBuilder.StructureMap;
    using ObjectBuilder.Unity;
    using Support;

    public class AllBuilders:ScenarioDescriptor
    {
        public AllBuilders()
        {
            Add(new RunDescriptor
            {
                Name = "Autofac builder",
                Settings =
                    new Dictionary<string, string>
                                {
                                    {
                                        "Builder",typeof(AutofacObjectBuilder).AssemblyQualifiedName
                                    }
                                }
            });

            Add(new RunDescriptor
            {
                Name = "Windsor builder",
                Settings =
                    new Dictionary<string, string>
                                {
                                    {
                                        "Builder",typeof(WindsorObjectBuilder).AssemblyQualifiedName
                                    }
                                }
            });

            Add(new RunDescriptor
            {
                Name = "Ninject builder",
                Settings =
                    new Dictionary<string, string>
                                {
                                    {
                                        "Builder",typeof(NinjectObjectBuilder).AssemblyQualifiedName
                                    }
                                }
            });


            Add(new RunDescriptor
            {
                Name = "Spring builder",
                Settings =
                    new Dictionary<string, string>
                                {
                                    {
                                        "Builder",typeof(SpringObjectBuilder).AssemblyQualifiedName
                                    }
                                }
            });

            Add(new RunDescriptor
            {
                Name = "StructureMap builder",
                Settings =
                    new Dictionary<string, string>
                                {
                                    {
                                        "Builder",typeof(StructureMapObjectBuilder).AssemblyQualifiedName
                                    }
                                }
            });

            Add(new RunDescriptor
            {
                Name = "Unity builder",
                Settings =
                    new Dictionary<string, string>
                                {
                                    {
                                        "Builder",typeof(UnityObjectBuilder).AssemblyQualifiedName
                                    }
                                }
            });

        }
    }
}