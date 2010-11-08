using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Core;

namespace NServiceBus.ObjectBuilder.Autofac.Internal
{
    ///<summary>
    /// Adopted from Autofac wiki:
    /// http://code.google.com/p/autofac/wiki/AddingToExistingContainer
    /// Workaround to enable recomposition of the container
    ///</summary>
    public class ContainerUpdater : ContainerBuilder
    {
        private readonly ICollection<Action<IComponentRegistry>> configurationActions = new List<Action<IComponentRegistry>>();

        ///<summary>
        ///</summary>
        ///<param name="configurationAction"></param>
        public override void RegisterCallback(Action<IComponentRegistry> configurationAction)
        {
            this.configurationActions.Add(configurationAction);
        }

        public void Update(IContainer container)
        {
            foreach (var action in this.configurationActions)
                action(container.ComponentRegistry);
        }
    }
}
