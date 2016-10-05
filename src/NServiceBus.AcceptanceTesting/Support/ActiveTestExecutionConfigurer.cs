namespace NServiceBus.AcceptanceTesting.Support
{
    using System.Collections.Generic;
    using System.Linq;

    public class ActiveTestExecutionConfigurer : List<IConfigureEndpointTestExecution>
    {
        public override string ToString()
        {
            return string.Join("; ", this.Select(t => t.GetType().Name));
        }
    }
}