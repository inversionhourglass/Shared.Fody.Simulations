using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;

namespace Fody.Simulations
{
    public class FakeArgumentSimulation(ILoadable loadable, ParameterDefinition parameterDef, SimulationModuleWeaver moduleWeaver) : ArgumentSimulation(parameterDef, moduleWeaver)
    {
        public override TypeSimulation Type => loadable.Type;

        public override IList<Instruction> Load() => loadable.Load();
    }
}
