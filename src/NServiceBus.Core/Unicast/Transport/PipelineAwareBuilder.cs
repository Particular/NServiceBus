namespace NServiceBus.Unicast.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.ObjectBuilder;

    class PipelineAwareBuilder : IBuilder
    {
        readonly PipelineInfo pipelineInfo;
        readonly IBuilder innerBuilder;

        public PipelineAwareBuilder(PipelineInfo pipelineInfo, IBuilder innerBuilder)
        {
            this.pipelineInfo = pipelineInfo;
            this.innerBuilder = innerBuilder;
        }

        object Enrich(object built)
        {
            var require = built as IRequirePipelineInfo;
            if (require != null)
            {
                require.SetPipelineInfo(pipelineInfo);
            }
            return built;
        }

        public void Dispose()
        {
            //Injected by Janitor
        }

        public object Build(Type typeToBuild)
        {
            return Enrich(innerBuilder.Build(typeToBuild));
        }

        public IBuilder CreateChildBuilder()
        {
            return innerBuilder.CreateChildBuilder();
        }

        public T Build<T>()
        {
            return (T) Enrich(innerBuilder.Build(typeof(T)));
        }

        public IEnumerable<T> BuildAll<T>()
        {
            return innerBuilder.BuildAll(typeof(T)).Select(Enrich).Cast<T>().ToArray();
        }

        public IEnumerable<object> BuildAll(Type typeToBuild)
        {
            return innerBuilder.BuildAll(typeToBuild).Select(Enrich).ToArray();
        }

        public void Release(object instance)
        {
            innerBuilder.Release(instance);
        }

        public void BuildAndDispatch(Type typeToBuild, Action<object> action)
        {
            innerBuilder.BuildAndDispatch(typeToBuild, o => action(Enrich(o)));
        }
    }
}