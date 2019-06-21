namespace NServiceBus
{
    using System;
    using ObjectBuilder;

    class BuilderReference
    {
        IBuilder value;

        public void SetBuilder(IBuilder builder)
        {
            if (value != null)
            {
                throw new Exception("Builder already set");
            }

            value = builder;
        }

        public IBuilder GetBuilder()
        {
            if (value == null)
            {
                throw new Exception("The endpoint has not been provided with DI infrastructure yet.");
            }

            return value;
        }
    }
}