using System;
using System.Web.Mvc;

namespace Host
{
    public class BasicViewPageActivator : IViewPageActivator
    {
        public object Create(ControllerContext controllerContext, Type type)
        {
            return Activator.CreateInstance(type);
        }
    }
}