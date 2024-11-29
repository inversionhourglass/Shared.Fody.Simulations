using Mono.Cecil.Cil;
using System.Collections.Generic;

namespace Fody.Simulations.Operations
{
    public abstract class UnaryOperation(ILoadable value) : Operation
    {
        protected ILoadable Value => value;

        public override OpCode TrueToken => value.TrueToken;

        public override OpCode FalseToken => value.FalseToken;

        public override SimulationModuleWeaver ModuleWeaver => value.ModuleWeaver;

        public override IList<Instruction> Load() => value.Load();
    }
}
