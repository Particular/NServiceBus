using System;
using System.Collections.Generic;
using System.Linq;

namespace NServiceBus.Core.Tests.Pipeline
{
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    [TestFixture]
    class PipelineMapTest
    {
        int typeIndex;
        Dictionary<Type, int> typeMap = new Dictionary<Type, int>();

        [Test]
        [Explicit]
        public void Can_draw_map_of_the_pipelines()
        {
            var behaviors = typeof(IBehavior).Assembly.GetTypes()
                .Where(t => t.GetInterfaces().Contains(typeof(IBehavior)))
                .Where(t => !t.IsInterface && !t.IsAbstract && !t.IsGenericTypeDefinition)
                .Where(t => t.BaseType != null && t.BaseType != typeof(SatelliteBehavior))
                .ToList();

            behaviors.Remove(typeof(NativeSubscribeTerminator));
            behaviors.Remove(typeof(NativeUnsubscribeTerminator));
            behaviors.Remove(typeof(MulticastPublishRouterBehavior));
            behaviors.Remove(typeof(InvokeSagaNotFoundBehavior));

            var registerStepTypes = typeof(IBehavior).Assembly.GetTypes()
                .Where(t => t.BaseType == typeof(RegisterStep));

            var registerStepTypesWithDefaultCtor = registerStepTypes.Where(
                t => t.GetConstructors().Any(c => c.GetParameters().Length == 0))
                .ToArray();

            var registerSteps = registerStepTypesWithDefaultCtor
                .Select(Activator.CreateInstance)
                .Cast<RegisterStep>()
                .ToArray();
            
            var behaviorsWithoutRegisterSteps = behaviors.Where(b => registerSteps.All(s => s.BehaviorType != b)).ToArray();

            var coordinator = new StepRegistrationsCoordinator(new List<RemoveStep>(), new List<ReplaceBehavior>());
            var additions = new List<RegisterStep>();
            foreach (var registerStep in registerSteps.Where(t => t.BehaviorType.BaseType != typeof(SatelliteBehavior)))
            {
                additions.Add(registerStep);
                coordinator.Register(registerStep);
            }
            foreach (var behaviorType in behaviorsWithoutRegisterSteps)
            {
                additions.Add(RegisterStep.Create(behaviorType.Name.Replace("Behavior", ""), behaviorType, behaviorType.Name));
                coordinator.Register(behaviorType.Name.Replace("Behavior",""), behaviorType, behaviorType.Name);
            }
            
            var allContextTypes = typeof(IBehavior).Assembly.GetTypes()
                .Where(x => IsContextType(x) && !x.IsAbstract);

            var pipes = allContextTypes.Select(c => GetPipe(c, additions)).Where(p => p != null).ToArray();

            var nodes = pipes.SelectMany(p => p.Steps).Select(s => s.Type).ToList();
            var edges = new List<Edge>();

            foreach (var pipe in pipes)
            {
                for (var index = 0; index < pipe.Steps.Length - 1; index++)
                {
                    edges.Add(new Edge
                    {
                        From = pipe.Steps[index].Type,
                        To = pipe.Steps[index+1].Type,
                    });
                }
                if (pipe.OutputType != null)
                {
                    edges.Add(new Edge
                    {
                        From = pipe.Steps.Last().Type,
                        To = pipes.Single(p => p.InputType == pipe.OutputType).Steps.First().Type
                    });
                }
                edges.AddRange(from step in pipe.Steps
                    from connection in step.Connections
                    select new Edge
                    {
                        From = step.Type,
                        To = pipes.Single(p => p.InputType == connection).Steps.First().Type
                    });
            }


            var json = $@"
{{
""nodes"": [	
{string.Join(",", nodes.Select(n => $@"
{{
""id"": {MapType(n)},
""title"": ""{n.Name}"",
""type"": ""{n.GetInputContext()}"",
""x"": 10,
""y"": 210
}}
"))}
],
""links"": [
{string.Join(",", edges.Select(e => $@"
{{
""source"": {MapType(e.From)},
""target"": {MapType(e.To)},
""left"": false, 
""right"": true
}}
"))}
]
}}
";
            Console.WriteLine(json);
        }

        private int MapType(Type t)
        {
            int result;
            if (!typeMap.TryGetValue(t, out result))
            {
                typeMap[t] = typeIndex;
                result = typeIndex;
                typeIndex++;
            }
            return result;
        }

        static bool IsContextType(Type type)
        {
            if (type.BaseType == null)
            {
                return false;
            }
            if (type == typeof(object))
            {
                return false;
            }
            if (type.BaseType == typeof(BehaviorContext))
            {
                return true;
            }
            return IsContextType(type.BaseType);
        }

        static Pipe GetPipe(Type contextType, List<RegisterStep> registerSteps)
        {
            var body = registerSteps.Where(s => !s.IsStageConnector() && s.GetInputContext() == contextType).ToArray();
            var tail = registerSteps.Where(s => s.IsStageConnector() && s.GetInputContext() == contextType).ToArray();

            if (tail.Length == 0)
            {
                return null;
            }

            var model = new PipelineModelBuilder(contextType, tail.Concat(body).ToList(), new List<RemoveStep>(), new List<ReplaceBehavior>());
            var steps = model.Build();

            return new Pipe
            {
                InputType = contextType,
                OutputType = typeof(IPipelineTerminator).IsAssignableFrom(tail[0].BehaviorType) ? null : tail[0].BehaviorType.GetOutputContext(),
                Steps = steps.Select(s => new Step
                {
                    Type = s.BehaviorType,
                    Connections = GetConnections(s)
                }).ToArray()
            };
        }

        static Type[] GetConnections(RegisterStep s)
        {
            return s.BehaviorType.GetConstructors().SelectMany(c => c.GetParameters()).Select(p => p.ParameterType).Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IPipeInlet<>)).Select(t => t.GetGenericArguments()[0]).ToArray();
        }

        class Pipe
        {
            public Type InputType;
            public Type OutputType;
            public Step[] Steps;
        }

        class Step
        {
            public Type Type;
            public Type[] Connections;
        }

        class Edge
        {
            public Type From;
            public Type To;
        }
    }
}
